# The Ledger

A 2D side-view fishing game with physically simulated rods, lines, reels and water, and hundreds of real fish
species that behave, feed and fight like they do in nature. You're the new survey angler at a broke field station
on Florida's Nature Coast. A vanished naturalist's field guide, the Ledger, sends you to real waters around the
world, from Lake of the Woods to Kona, and finally to a night drop over the coelacanth caves of the Comoros.

**Status:** design and build plan complete; development starts at Milestone M0.

- Build plan and design docs: [`docs/`](docs/)
- Rules for anyone (human or AI) working on the code: [`CLAUDE.md`](CLAUDE.md)
- The owner's design decisions: [`docs/OPEN_QUESTIONS.md`](docs/OPEN_QUESTIONS.md)

## Stack

Godot 4 (.NET) · C# · a custom deterministic simulation library (`sim/`) · xUnit · GitHub Actions.
