using TheLedger.Sim.Fish;

namespace TheLedger.Sim.World;

/// <summary>One landed fish (the temporary Ledger until M7).</summary>
public struct Catch
{
    public int Angler;
    public SpeciesSpec Species;
    public float LengthCm, WeightKg, Hour;
    public string Terminal;
    public long Tick;
    public int FishId;
    /// <summary>Still in the angler's hands, waiting for keep or release.</summary>
    public bool Pending;
    public bool Kept;
}
