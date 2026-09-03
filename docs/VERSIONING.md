# Versioning and stability

`eBuildingBlocks` ships as six independently-versioned NuGet packages rather than one
monolithic package. This document explains how versioning works across them, what
"breaking" means here, and which combinations are known to work together — so you can
judge upgrade risk before pulling in a new version.

## Independent SemVer per package

Each package (`Domain`, `Application`, `Common`, `Infrastructure`, `API`, `EventBus`,
`SMPP`) has its own `<Version>` and follows [Semantic Versioning](https://semver.org/)
independently:

- **PATCH** (`3.1.0` → `3.1.1`) — bug fixes with no public API change.
- **MINOR** (`3.1.0` → `3.2.0`) — additive changes only (new types, new optional
  parameters, new extension methods). Existing code keeps compiling and behaving the
  same.
- **MAJOR** (`4.0.0` → `5.0.0`) — a public API removed, renamed, or changed behavior in
  a way that can break existing callers.

A minor bump in `Domain` does not imply anything about `Infrastructure`'s version, and
vice versa — check the specific package you depend on.

## Compatibility matrix

These are the versions this repository builds, tests, and ships together as of this
writing. Older combinations may still work but aren't actively verified.

| Package | Version | Depends on (this repo) |
|---|---|---|
| `eBuildingBlocks.Domain` | 3.2.0 | — |
| `eBuildingBlocks.Common` | 3.2.0 | — |
| `eBuildingBlocks.Application` | 3.1.0 | `Domain`, `Common` |
| `eBuildingBlocks.EventBus` | 3.2.0 | `Common` |
| `eBuildingBlocks.Infrastructure` | 5.0.0 | `Domain`, `Application`, `Common` |
| `eBuildingBlocks.API` | 3.1.0 | `Application`, `Infrastructure` |
| `eBuildingBlocks.SMPP` | 3.1.0 | — |

If you reference `eBuildingBlocks.API`, NuGet resolves `Application` and
`Infrastructure` (and their own dependencies) transitively — you don't need to pin
those yourself unless you have a specific reason to.

## How breaking changes are communicated

Every breaking change:

1. Bumps the affected package's **major** version.
2. Gets an entry in [`CHANGELOG.md`](../CHANGELOG.md) under that package, marked
   **— Breaking**, explaining exactly what changed and who is affected.
3. Gets a migration note when the fix isn't a one-line rename — see
   [`docs/MIGRATION_OUTBOX_PUBLISHER.md`](MIGRATION_OUTBOX_PUBLISHER.md) for an example
   of the level of detail we aim for.

Where feasible, a breaking removal is preceded by a **deprecation window**: the old API
keeps working (usually with a runtime warning), and `CHANGELOG.md` has a
**Deprecations** section naming the version it'll be removed in. The outbox publisher
migration (`IEventPublisher` → `IOutboxIntegrationPublisher`) is the current example of
this pattern.

## What this means for you

- Pin exact or narrow version ranges in production (`Version="5.0.0"`, not `Version="5.*"`)
  and upgrade deliberately — read the `CHANGELOG.md` entry for the package before bumping,
  especially across a major version.
- A minor or patch bump should never require code changes on your side. If it does,
  that's a bug in our versioning — please open an issue.
- Breaking changes are not frequent by design, but this is still a pre-1.0-in-spirit
  project undergoing active architectural cleanup (see
  [`TODO.md`](../TODO.md) and [`docs/ARCHITECTURE_ANALYSIS.md`](ARCHITECTURE_ANALYSIS.md)
  for what's tracked). Expect more major bumps than a mature, stable library while that
  work lands — each one will be documented as above, not silent.
