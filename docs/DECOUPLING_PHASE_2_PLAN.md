# eBuildingBlocks — NuGet Package Decoupling: Phase 2 Plan

**Target repo/branch:** `ghazi1567/eBuildingBlocks` @ `latest-dotnet-10`
**Audience:** Claude Code (execute directly against a working checkout)
**Depends on:** `docs/DECOUPLING_IMPLEMENTATION_PLAN.md` (Phase 1) already shipped and
released to consumers.
**This is a breaking major-version change, by design.** Phase 1 made it optional to
migrate; Phase 2 makes the migration mandatory by removing the fallback it was built
around. Do not start this task until the gate in section 1 is satisfied.

---

## 0. Objective

Phase 1 added `IOutboxIntegrationPublisher` and made the outbox processor prefer it,
while silently falling back to the legacy `IEventPublisher` (from
`eBuildingBlocks.EventBus`) so nothing broke. That fallback is the *only* remaining
reason `eBuildingBlocks.Infrastructure` still references `eBuildingBlocks.EventBus`,
and therefore the only remaining reason installing `Infrastructure` still drags in
MassTransit and RabbitMQ.Client transitively.

Phase 2 removes that fallback and the reference behind it. After this task,
`eBuildingBlocks.Infrastructure` has zero knowledge of MassTransit/RabbitMQ, and the
target dependency graph from the Phase 1 plan (section 1) is fully realized.

---

## 1. Gate — do not start until this is true

This task is **not** scheduled to a calendar date. It starts when:

- [ ] `eBuildingBlocks.Infrastructure` 3.x (the Phase 1 release) has been out for at
      least one full release cycle.
- [ ] There is no remaining evidence of the Phase 1 legacy-fallback warning
      (`"Outbox processor resolved IEventPublisher directly..."`) firing in any known
      consumer's logs. If you have telemetry/log aggregation access across consumer
      apps, check it. If you don't, treat direct confirmation from each known
      production consumer (that they've upgraded `eBuildingBlocks.EventBus` to >= 3.x
      and are no longer seeing the warning) as the substitute signal.
- [ ] All known production consumers have been notified in advance (changelog entry,
      release notes, or direct communication) that the next major version of
      `eBuildingBlocks.Infrastructure` will require `IOutboxIntegrationPublisher` to be
      registered explicitly.

**If this gate isn't satisfied, stop and report back instead of proceeding.** Phase 2
is intentionally allowed to break consumers who haven't migrated — but only consumers
who have had a real opportunity to migrate first.

---

## 2. Current vs. target state

**Before Phase 2 (current, Phase-1-only state):**
```
Infrastructure -> Application, Common, Domain, EventBus   <- kept only for the legacy fallback
```
`OutboxProcessorBackgroundService.ResolvePublisher` tries `IOutboxIntegrationPublisher`
first, falls back to `IEventPublisher` via `LegacyEventPublisherAdapter`, logs a
one-time warning on the fallback path.

**After Phase 2 (target):**
```
Infrastructure -> Application, Common, Domain              <- EventBus reference gone
```
`OutboxProcessorBackgroundService` requires `IOutboxIntegrationPublisher` directly. No
fallback, no adapter, no `EventBus` project reference, no MassTransit/RabbitMQ.Client
in `Infrastructure`'s transitive closure.

---

## 3. Task list (execute in order)

### Task 1 — Remove the legacy fallback code

**File:** `eBuildingBlocks.Infrastructure/Outbox/OutboxProcessorBackgroundService.cs`

- [ ] Delete `LegacyEventPublisherAdapter` (wherever it lives — inline in this file or
      in a sibling `LegacyEventPublisherAdapter.cs`).
- [ ] Delete the `_legacyPublisherWarningLogged` field and the `ResolvePublisher`
      helper method added in Phase 1.
- [ ] Replace the resolution call in `ProcessOnceAsync` with a direct, required
      resolution:
  ```csharp
  var publisher = sp.GetRequiredService<IOutboxIntegrationPublisher>();
  ```
  `GetRequiredService` already throws a clear `InvalidOperationException` naming the
  missing service type if it isn't registered — this replaces the custom exception
  message Phase 1 threw in the "neither registered" branch. Keep the exception
  behavior but simplify: no custom message needed unless you want to add a one-line
  hint pointing at the migration guide (optional — see Task 4).
- [ ] Update the XML doc summary on `OutboxProcessorBackgroundService` to remove the
      "or, for backward compatibility, `IEventPublisher`..." sentence added in Phase 1;
      it should now read as a single, non-conditional description:
  ```csharp
  /// <summary>
  /// Polls <see cref="OutboxMessage"/> rows and publishes via <see cref="IOutboxIntegrationPublisher"/>.
  /// </summary>
  ```

**File:** `eBuildingBlocks.Infrastructure/Outbox/OutboxPublisherDispatch.cs`

- [ ] Confirm this file already only references `IOutboxIntegrationPublisher` (it
      should, from Phase 1 Task 4). No change expected here — just verify.

---

### Task 2 — Remove the `EventBus` project reference

**File:** `eBuildingBlocks.Infrastructure/eBuildingBlocks.Infrastructure.csproj`

- [ ] Delete:
  ```xml
  <ProjectReference Include="..\eBuildingBlocks.EventBus\eBuildingBlocks.EventBus.csproj" />
  ```
- [ ] Run `dotnet build eBuildingBlocks.Infrastructure` standalone and fix any residual
      `using BuildingBlocks.EventBus...` compile errors — there should be none if
      Task 1 was done correctly, since that was the only consumer.
- [ ] Run `dotnet list eBuildingBlocks.Infrastructure package --include-transitive` (or
      equivalent) and confirm `MassTransit`, `MassTransit.RabbitMQ`, and
      `RabbitMQ.Client` no longer appear anywhere in the output. This is the concrete,
      verifiable outcome of this entire two-phase effort — capture the before/after
      output in the PR description.

---

### Task 3 — Update the reference app and examples if needed

- [ ] `eBuildingBlocks.ReferenceApp.API` already has its own **direct**
      `ProjectReference` to `eBuildingBlocks.EventBus` (verified — it does not rely on
      the transitive one from `Infrastructure`). No change needed there, but rebuild it
      standalone to confirm.
- [ ] Grep the whole solution for any other project that references
      `BuildingBlocks.EventBus` types without an explicit `ProjectReference` /
      `PackageReference` of its own (i.e., anything that was silently relying on the
      transitive edge through `Infrastructure`). Add an explicit reference wherever
      found. Expected result: none found, since Task 2 in Phase 1 already confirmed
      `EventBus` usage was isolated to the two outbox files.

---

### Task 4 — Update the migration guide for Phase 2

**File:** `docs/MIGRATION_OUTBOX_PUBLISHER.md` (created in Phase 1 — extend, don't
replace)

- [ ] Update the "Timeline" section: mark the "Now (eBuildingBlocks.Infrastructure
      3.x)" line as historical, and change the "Next major version" line to reflect
      that this is now the current release:
  ```markdown
  ## Timeline
  - **eBuildingBlocks.Infrastructure 3.x (superseded):** both paths worked. Legacy
    path logged a warning once per process.
  - **eBuildingBlocks.Infrastructure 4.0 (current):** the `IEventPublisher` fallback
    and the `EventBus` project reference have been removed. Apps that have not
    registered `IOutboxIntegrationPublisher` will get a startup
    `InvalidOperationException` naming the missing service instead of a warning.
  ```
- [ ] Add a new top section, above "What changed", titled "Breaking in 4.0" that states
      plainly: if you're upgrading directly from `Infrastructure` 3.x or earlier and
      you use the outbox processor, you must have `IOutboxIntegrationPublisher`
      registered before calling `AddOutboxProcessor<TDbContext>()` — either via
      `eBuildingBlocks.EventBus`'s `AddIntegrationMassTransit(...)` (>= 3.x, which
      registers it automatically) or your own implementation. Without it, the app will
      fail to start with a clear exception rather than degrade silently.
- [ ] No other doc content changes — the "If you're using a broker other than
      MassTransit" section from Phase 1 is still accurate as-is.

---

### Task 5 — Changelog and versioning

- [ ] Add a `## [4.0.0] — eBuildingBlocks.Infrastructure` entry under the existing
      `## Deprecations` section (or a new `## Breaking Changes` section) in
      `CHANGELOG.md`:
  ```markdown
  ## Breaking Changes

  ### eBuildingBlocks.Infrastructure 4.0.0
  - Removed the deprecated `IEventPublisher` fallback in the outbox processor
    (deprecated since 3.1.0). `IOutboxIntegrationPublisher` must now be registered
    explicitly before starting the outbox processor.
  - Removed the `ProjectReference` to `eBuildingBlocks.EventBus`. Consumers who use
    the outbox + MassTransit combination and were relying on the transitive
    MassTransit/RabbitMQ.Client dependency must now reference
    `eBuildingBlocks.EventBus` directly.
  - See `docs/MIGRATION_OUTBOX_PUBLISHER.md` for the full migration path.
  ```
- [ ] Bump `eBuildingBlocks.Infrastructure` to the next **major** version
      (e.g. `3.x.x` -> `4.0.0`).
- [ ] No version change required for `Domain`, `Common`, `EventBus`, `Application`,
      `API`, or `SMPP` — none of their public APIs change in this task.

---

### Task 6 — Tests

- [ ] Delete or rewrite the Phase 1 tests that specifically exercised the legacy
      fallback (`LegacyEventPublisherAdapter` resolution, the one-time warning guard) —
      those code paths no longer exist.
- [ ] Add a test confirming `OutboxProcessorBackgroundService` throws
      `InvalidOperationException` on startup/first poll when
      `IOutboxIntegrationPublisher` is not registered, and that the exception message
      is clear enough to act on (either the default `GetRequiredService` message, or
      your added hint from Task 1 — assert on whichever you implemented).
- [ ] Keep/adapt the existing "modern path" test (resolves and uses
      `IOutboxIntegrationPublisher` correctly) — this should need no changes.
- [ ] Add a solution-wide check (can be a simple test or a build-time script) that
      fails if `eBuildingBlocks.Infrastructure.csproj` ever re-introduces a
      `ProjectReference` to `eBuildingBlocks.EventBus`, so this doesn't silently regress
      in a future change. A simple approach: a unit test that reflects over the built
      `eBuildingBlocks.Infrastructure.dll`'s referenced assemblies and asserts
      `MassTransit` and `RabbitMQ.Client` are not among them.
- [ ] Run the full solution build and test suite. Expect the `ReferenceApp.API`
      outbox-to-MassTransit flow to keep working unchanged (it has its own direct
      `EventBus` reference per Task 3).

---

## 4. Non-goals for this task

- Do not touch `Application`, `API`, `Domain`, `Common`, `EventBus`, or `SMPP` beyond
  what's explicitly listed above (Task 3's verification grep, if it finds nothing to
  change, ends there).
- Do not use this task as an opportunity to also decouple something else discovered
  along the way (e.g. any other `Infrastructure` -> package edge). If something new
  surfaces, note it separately for a future, distinct plan rather than folding it in
  here — this task's scope is exactly "remove the Phase 1 fallback and the reference
  behind it," nothing more.
- Do not attempt to auto-migrate consumers' code for them. The migration guide and the
  clear startup exception are the intended mechanisms; this repo doesn't own consumer
  codebases.

---

## 5. Versioning summary for this task

| Package | Change type | Version bump | Breaking? |
|---|---|---|---|
| `eBuildingBlocks.Infrastructure` | legacy fallback + EventBus reference removed | **major** | **Yes** |
| `eBuildingBlocks.Domain` | no change | — | — |
| `eBuildingBlocks.Common` | no change | — | — |
| `eBuildingBlocks.EventBus` | no change | — | — |
| `eBuildingBlocks.Application` | no change | — | — |
| `eBuildingBlocks.API` | no change | — | — |
| `eBuildingBlocks.SMPP` | no change | — | — |

Only consumers who use the outbox processor **and** never registered
`IOutboxIntegrationPublisher` (i.e., ignored the Phase 1 warning across a full release
cycle, or skipped straight from a pre-Phase-1 version to this one) are affected, and
the effect is a clear startup exception with a link to the migration guide — not a
silent failure or a subtle behavior change.
