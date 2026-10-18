namespace TheLedger.Sim.Core;

/// <summary>
/// Discrete things that happened in the sim, for audio, UI, camera and haptics to react to
/// (docs/02_TECH_STACK.md §3.1). Grows as systems are added.
/// </summary>
public enum SimEventType : byte
{
    None = 0,
    /// <summary>A NaN/Inf was found and the state was restored. Tests assert there are none.</summary>
    SimFault,
    LureEntered,
    LureSkipped,
    Backlash,
    Snagged,
    SnagFreed,
    Bite,
    HookSet,
    HookPulled,
    LineBroke,
    RodBroke,
    FishJumped,
    FishLanded,
    Splash,
    /// <summary>A fish lunged at a lure or bait (Id = fish id).</summary>
    FishStrike,
    /// <summary>A fish let go of the bait it was holding (Id = fish id).</summary>
    FishSpit,
    /// <summary>Tried to land a fish that wasn't ready or close enough (Id = fish id).</summary>
    LandFailed,
}

/// <summary>One event. Plain data, no references, so the buffer never allocates.</summary>
public struct SimEvent
{
    public SimEventType Type;
    public long Tick;
    /// <summary>The angler it concerns, or -1 for world events.</summary>
    public int Angler;
    /// <summary>An id (fish id, node index...) whose meaning depends on the type.</summary>
    public int Id;
    public Vec2 Position;
    /// <summary>A magnitude (energy, force, speed...) whose meaning depends on the type.</summary>
    public float Value;

    public override string ToString() => $"{Type} t={Tick} a={Angler} id={Id} pos={Position} v={Value:0.###}";
}

/// <summary>
/// Fixed-capacity event queue. The sim pushes during Step; the game layer drains after each tick.
/// When full, the oldest events are overwritten and counted, never reallocated.
/// </summary>
public sealed class SimEventQueue
{
    private readonly SimEvent[] _items;
    private int _start;
    private int _count;

    public SimEventQueue(int capacity = 1024)
    {
        _items = new SimEvent[capacity];
    }

    public int Count => _count;
    public int Capacity => _items.Length;
    /// <summary>Events lost because nobody drained the queue in time.</summary>
    public long Dropped { get; private set; }

    public void Push(in SimEvent e)
    {
        if (_count == _items.Length)
        {
            _items[_start] = e;
            _start = (_start + 1) % _items.Length;
            Dropped++;
            return;
        }
        _items[(_start + _count) % _items.Length] = e;
        _count++;
    }

    public bool TryPop(out SimEvent e)
    {
        if (_count == 0)
        {
            e = default;
            return false;
        }
        e = _items[_start];
        _start = (_start + 1) % _items.Length;
        _count--;
        return true;
    }

    public void Clear()
    {
        _start = 0;
        _count = 0;
    }
}
