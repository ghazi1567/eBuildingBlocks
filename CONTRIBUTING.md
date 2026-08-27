# Contributing to eBuildingBlocks

Thanks for considering a contribution — community PRs are what will make this project useful beyond a single team. This guide covers the basics for getting a change in.

## Getting set up

```bash
git clone https://github.com/ghazi1567/eBuildingBlocks.git
cd eBuildingBlocks
dotnet restore ./eBuildingBlocks.sln
dotnet build ./eBuildingBlocks.sln
dotnet test ./eBuildingBlocks.sln
```

There's no database or external service required to build and run the unit tests. To exercise the framework end-to-end, run one of the reference apps:

```bash
dotnet run --project ./eBuildingBlocks.ReferenceApp.API
```

## Project layout

Each top-level `eBuildingBlocks.*` folder is an independent NuGet package (see `CLAUDE.md` / `README.md` for the layer breakdown). Keep changes scoped to the layer they belong to — e.g. don't add EF Core–specific code to `eBuildingBlocks.Domain`, which must stay persistence-agnostic.

## Making a change

1. Fork the repo and create a branch off `latest-dotnet-10` (the active development branch).
2. Keep PRs focused — one feature or fix per PR is easier to review than a bundle of unrelated changes.
3. Add or update unit tests under `eBuildingBlocks.Infrastructure.Tests` (or a new test project) for behavior you add or change.
4. Update the relevant doc under `docs/` and the README if you change public APIs or add a feature.
5. Make sure `dotnet build` and `dotnet test` pass locally before opening the PR — CI runs the same commands and will block merge on failure.
6. Open the PR against `latest-dotnet-10` and describe *why* the change is needed, not just what changed.

## Coding conventions

- Nullable reference types are enabled — don't suppress warnings with `!` unless you've actually verified non-null.
- Follow existing naming and folder conventions within each layer (see `ARCHITECTURAL_REVIEW.md` for the domain-event design rationale).
- Domain events (`IDomainEvent`) are in-process/same-transaction; integration events (`IntegrationEvent`) are cross-service via MassTransit — don't conflate the two.
- Breaking changes to public interfaces should be called out explicitly in the PR description and in `CHANGELOG.md`.

## Reporting bugs / requesting features

Open a GitHub issue with a minimal repro (a failing test is ideal) or a clear description of the use case a new feature would unblock. If you're planning a larger change (a new package, a new cross-cutting abstraction), open an issue first to discuss the approach before investing in the implementation.

## Questions

If something in this guide or the docs is unclear, open an issue — that's a sign the docs need improving, and issues are more discoverable than one-off answers.
