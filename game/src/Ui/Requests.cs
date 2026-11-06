using Godot;
using TheLedger.SimGlue;

namespace TheLedger.Ui;

/// <summary>One of the day's requests on Junie's board at the bait stand.</summary>
public sealed class Request
{
    public string Kind = "";        // size | release | variety | night
    public string Species = "", Name = "", Spot = "", SpotName = "";
    public float MinCm;
    public int Count;
    public float Reward;
    public string Text = "";
}

/// <summary>
/// Junie's board (docs/01 §5: Junie Alcott runs the bait stand and keeps a running bet on the biggest fish): three
/// small requests a day, drawn from the fish at the spots you can reach, paid on the spot when done. Progress is
/// read from the Ledger (each entry carries its game day), so nothing extra can get out of sync. The board for a
/// day is the same every time you look (seeded by the day).
/// </summary>
public static class Requests
{
    private static int _day = int.MinValue;
    private static List<Request> _today = new();

    public static List<Request> Today(SimDriver driver)
    {
        int day = driver.World.Day;
        if (day == _day) return _today;
        _day = day;
        _today = Generate(driver, day);
        return _today;
    }

    private static List<Request> Generate(SimDriver driver, int day)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)(day * 7919 + 17) };
        // Every species at every spot you can reach, with where it lives.
        var pool = new List<(TheLedger.Sim.Fish.SpeciesSpec sp, string spot, string spotName)>();
        foreach (var s in SpotInfo.All())
        {
            if (!Story.SpotOpen(s.Id)) continue;
            foreach (var e in TheLedger.Sim.Fish.SpotStock.Load(s.Json).Stock)
                if (driver.Species.FirstOrDefault(x => x.Id == e.Species) is { } sp && pool.All(p => p.sp.Id != sp.Id || p.spot != s.Id))
                    pool.Add((sp, s.Id, s.Name));
        }
        var list = new List<Request>();
        if (pool.Count == 0) return list;

        // 1. A size bet: a decent fish of one species, anywhere.
        var (bs, _, _) = pool[rng.RandiRange(0, pool.Count - 1)];
        float typTop = bs.Size.LengthCm.Typical.Length > 1 ? bs.Size.LengthCm.Typical[1] : 20f;
        float cap = bs.Size.Growth.Linf_cm > 0f ? MathF.Min(typTop, bs.Size.Growth.Linf_cm * 0.85f) : typTop;
        float typLow = bs.Size.LengthCm.Typical.Length > 0 ? bs.Size.LengthCm.Typical[0] : cap * 0.6f;
        float minCm = MathF.Round(Mathf.Lerp(typLow, cap, rng.RandfRange(0.45f, 0.7f)));
        list.Add(new Request
        {
            Kind = "size", Species = bs.Id, Name = bs.CommonName, MinCm = minCm,
            Reward = MathF.Round(2f + minCm * 0.12f), Text = $"A {bs.CommonName.ToLowerInvariant()} of {minCm:0} cm or more",
        });

        // 2. The Trust's tag count: release a few of one species at a given spot.
        var (rs, rspot, rname) = pool[rng.RandiRange(0, pool.Count - 1)];
        int n = rng.RandiRange(2, 3);
        list.Add(new Request
        {
            Kind = "release", Species = rs.Id, Name = rs.CommonName, Spot = rspot, SpotName = rname, Count = n,
            Reward = 1f + 1.5f * n, Text = $"Release {n} {Plural(rs.CommonName, n)} at {rname}",
        });

        // 3. Either a variety day or, if there are whiskered fish about, a night fish.
        var night = pool.Where(p => p.sp.Id is "channel_catfish" or "brown_bullhead" or "american_eel").ToList();
        if (night.Count > 0 && day % 2 == 1)
        {
            var (ns, nspot, nname) = night[rng.RandiRange(0, night.Count - 1)];
            list.Add(new Request
            {
                Kind = "night", Species = ns.Id, Name = ns.CommonName, Spot = nspot, SpotName = nname, Reward = 8f,
                Text = $"A {ns.CommonName.ToLowerInvariant()} after dark at {nname}",
            });
        }
        else
        {
            int k = rng.RandiRange(3, 4);
            list.Add(new Request { Kind = "variety", Count = k, Reward = 1.5f * k, Text = $"{k} different species today" });
        }
        return list;
    }

    /// <summary>Anglers say "three bass", "two bluegill": most fish names don't change; a few take an s.</summary>
    private static string Plural(string name, int n)
    {
        string s = name.ToLowerInvariant();
        return n > 1 && (s.EndsWith("shiner") || s.EndsWith("eel") || s.EndsWith("bullhead") || s.EndsWith("crappie")) ? s + "s" : s;
    }

    private static bool IsNight(float hour) => hour < 5.5f || hour > 20.5f;

    /// <summary>How far along a request is today: (have, need).</summary>
    public static (int have, int need) Progress(Request r, int day)
    {
        var today = LedgerStore.Entries.Where(e => e.Day == day).ToList();
        return r.Kind switch
        {
            "size" => (today.Any(e => e.Species == r.Species && e.LengthCm >= r.MinCm) ? 1 : 0, 1),
            "release" => (Math.Min(r.Count, today.Count(e => e.Species == r.Species && e.Spot == r.Spot && !e.Kept)), r.Count),
            "night" => (today.Any(e => e.Species == r.Species && e.Spot == r.Spot && IsNight(e.GameHour)) ? 1 : 0, 1),
            "variety" => (Math.Min(r.Count, today.Select(e => e.Species).Distinct().Count()), r.Count),
            _ => (0, 1),
        };
    }

    /// <summary>
    /// After each Ledger entry: pays for any request that just got done (once per request per day, saved in the
    /// profile) and returns them so the game can say so.
    /// </summary>
    public static List<Request> CheckNewlyDone(SimDriver driver)
    {
        var p = ProfileStore.Current;
        int day = driver.World.Day;
        if (p.BoardDay != day) { p.BoardDay = day; p.BoardDone.Clear(); }
        var done = new List<Request>();
        var list = Today(driver);
        for (int i = 0; i < list.Count; i++)
        {
            if (p.BoardDone.Contains(i)) continue;
            var (have, need) = Progress(list[i], day);
            if (have < need) continue;
            p.BoardDone.Add(i);
            p.Money += list[i].Reward;
            done.Add(list[i]);
        }
        if (done.Count > 0) ProfileStore.Save();
        return done;
    }

    public static bool IsDone(int index, int day) => ProfileStore.Current.BoardDay == day && ProfileStore.Current.BoardDone.Contains(index);
}

/// <summary>Junie's board on the HUD (top right, under the clock): today's three requests, ticks, rewards and your money.</summary>
public partial class RequestsHud : Control
{
    public SimDriver Driver { get; set; } = null!;

    private static readonly Color Panel = new(0.04f, 0.07f, 0.09f, 0.62f);
    private static readonly Color Dim = new(0.78f, 0.82f, 0.84f);
    private static readonly Color Gold = new(1f, 0.84f, 0.45f);
    private static readonly Color Green = new(0.55f, 0.9f, 0.6f);
    private int _key;
    private float _flash;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>A request was just done: the board glows for a moment.</summary>
    public void Flash() => _flash = 1f;

    public override void _Process(double delta)
    {
        _flash = MathF.Max(0f, _flash - (float)delta * 0.6f);
        int day = Driver.World.Day;
        var list = Requests.Today(Driver);
        int key = HashCode.Combine(day, LedgerStore.Entries.Count, (int)(ProfileStore.Current.Money * 100f), ProfileStore.Current.BoardDone.Count, (int)(_flash * 20f), (int)GetViewportRect().Size.X);
        if (key != _key) { _key = key; QueueRedraw(); }
        _ = list;
    }

    public override void _Draw()
    {
        var list = Requests.Today(Driver);
        int day = Driver.World.Day;
        var font = UiFonts.Body;
        var bold = UiFonts.Bold;
        const float w = 380f, pad = 12f, rowH = 24f;
        float h = pad * 2f + 26f + rowH * list.Count;
        float vw = GetViewportRect().Size.X;
        var box = new Rect2(vw - 16f - w, 52f, w, h);
        DrawRect(box, Panel.Lerp(new Color(0.35f, 0.28f, 0.08f, 0.8f), _flash * 0.6f));
        float x = box.Position.X + pad, y = box.Position.Y + pad;
        DrawString(bold, new Vector2(x, y + 16f), "JUNIE'S BOARD", HorizontalAlignment.Left, -1, 15, Gold);
        DrawString(bold, new Vector2(x, y + 16f), $"${ProfileStore.Current.Money:0.00}", HorizontalAlignment.Right, w - 2f * pad, 17, Colors.White);
        y += 26f;
        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            bool done = Requests.IsDone(i, day);
            var (have, need) = Requests.Progress(r, day);
            string box2 = done ? "✓" : "○";
            var c = done ? Green : Dim;
            DrawString(bold, new Vector2(x, y + 17f), box2, HorizontalAlignment.Left, -1, 16, c);
            string prog = need > 1 && !done ? $"  {have}/{need}" : "";
            DrawString(font, new Vector2(x + 20f, y + 17f), r.Text + prog, HorizontalAlignment.Left, w - 2f * pad - 70f, 15, c);
            DrawString(bold, new Vector2(x, y + 17f), done ? "paid" : $"${r.Reward:0}", HorizontalAlignment.Right, w - 2f * pad, 15, done ? Green : Gold);
            y += rowH;
        }
    }
}
