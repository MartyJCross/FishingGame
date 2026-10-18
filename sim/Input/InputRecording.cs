using System.Text;

namespace TheLedger.Sim.Input;

/// <summary>
/// A recorded session: header (format version, sim version tag, seed, tick rate, angler count) and then, per tick,
/// one InputFrame per angler. File extension: .rec. Used for tests (tests/replays/), bug repros and screenshots.
/// </summary>
public sealed class InputRecording
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TLREC");
    public const ushort FormatVersion = 1;

    public ulong Seed { get; init; }
    public int TickHz { get; init; } = 120;
    public int AnglerCount { get; init; } = 1;
    /// <summary>Free text: which scene/spot and rig the recording expects, e.g. "scene=pond_test".</summary>
    public string Setup { get; init; } = "";

    /// <summary>Frames in tick order; frame for (tick t, angler a) is at index t * AnglerCount + a.</summary>
    public List<InputFrame> Frames { get; } = new();

    public int TickCount => AnglerCount == 0 ? 0 : Frames.Count / AnglerCount;

    public ReadOnlySpan<InputFrame> TickFrames(int tick) =>
        System.Runtime.InteropServices.CollectionsMarshal.AsSpan(Frames).Slice(tick * AnglerCount, AnglerCount);

    public void AddTick(ReadOnlySpan<InputFrame> perAngler)
    {
        if (perAngler.Length != AnglerCount)
            throw new ArgumentException($"Expected {AnglerCount} frames, got {perAngler.Length}");
        foreach (var f in perAngler) Frames.Add(f);
    }

    public void Save(Stream s)
    {
        using var w = new BinaryWriter(s, Encoding.UTF8, leaveOpen: true);
        w.Write(Magic);
        w.Write(FormatVersion);
        w.Write(Seed);
        w.Write(TickHz);
        w.Write(AnglerCount);
        w.Write(Setup);
        w.Write(TickCount);
        foreach (var f in Frames) f.Write(w);
    }

    public void Save(string path)
    {
        using var fs = File.Create(path);
        Save(fs);
    }

    public static InputRecording Load(Stream s)
    {
        using var r = new BinaryReader(s, Encoding.UTF8, leaveOpen: true);
        byte[] magic = r.ReadBytes(Magic.Length);
        if (!magic.AsSpan().SequenceEqual(Magic)) throw new InvalidDataException("Not a .rec file");
        ushort version = r.ReadUInt16();
        if (version != FormatVersion) throw new InvalidDataException($"Unsupported .rec version {version}");
        var rec = new InputRecording
        {
            Seed = r.ReadUInt64(),
            TickHz = r.ReadInt32(),
            AnglerCount = r.ReadInt32(),
            Setup = r.ReadString(),
        };
        int ticks = r.ReadInt32();
        for (int i = 0; i < ticks * rec.AnglerCount; i++) rec.Frames.Add(InputFrame.Read(r));
        return rec;
    }

    public static InputRecording Load(string path)
    {
        using var fs = File.OpenRead(path);
        return Load(fs);
    }
}
