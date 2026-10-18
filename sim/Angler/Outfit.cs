using TheLedger.Sim.Tackle;

namespace TheLedger.Sim.Angler;

/// <summary>A rod, reel and line rigged together (game/data/gear/combos/*.json).</summary>
public sealed class Outfit
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public RodSpec Rod { get; init; } = RodSpec.MediumSpinning();
    public ReelSpec Reel { get; init; } = ReelSpec.Spinning2500();
    public LineSpec Line { get; init; } = LineSpec.Mono10();
}
