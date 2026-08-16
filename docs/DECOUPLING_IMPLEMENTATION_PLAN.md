# eBuildingBlocks — NuGet Package Decoupling Implementation Plan

**Target repo/branch:** `ghazi1567/eBuildingBlocks` @ `latest-dotnet-10`
**Audience:** Claude Code (execute directly against a working checkout)
**Constraint:** These packages are already consumed by production applications.
**No public API removal, no forced upgrades, no runtime breakage in this release.**
Deprecate first, remove later, always with a migration path.

---

## 0. Objective

Today, installing `eBuildingBlocks.Infrastructure` transitively pulls in MassTransit +
RabbitMQ.Client (via a hard `ProjectReference` to `eBuildingBlocks.EventBus`), even for
consumers who only want the EF repository, unit of work, audit interceptor, or
multi-tenancy — none of which touch a message broker.

Goal: make every package pull in only what it truly needs, using the smallest possible
change, delivered as a **two-phase, backward-compatible rollout**:

- **Phase 1 (this release, minor/non-breaking):** introduce the decoupled path
  alongside the existing one. Nothing breaks. Old code paths keep working but are
  marked `[Obsolete]` with a clear migration message and a doc link.
- **Phase 2 (a future major version, separately scheduled — not part of this task):**
  remove the obsolete fallback and the `EventBus` reference from `Infrastructure`
  entirely. Not implemented now; this plan only prepares for it.

---

## 1. Current vs. target dependency graph

**Current:**
```
Common          (leaf)
Domain          -> Common                [unused reference — nothing in Domain touches Common]
Application     -> Common, Domain
EventBus        -> Common
Infrastructure  -> Application, Common, Domain, EventBus   <- forces MassTransit/RabbitMQ on every consumer
API             -> Application, Infrastructure
SMPP            (leaf, already independent — no change)
```

**Target (after Phase 2, for reference only):**
```
Common          (leaf)
Domain          (leaf)
Application     -> Common, Domain
EventBus        -> Common
Infrastructure  -> Application, Common, Domain              <- MassTransit/RabbitMQ gone
API             -> Application, Infrastructure, EventBus
SMPP            (leaf, unchanged)
```

**This task delivers Phase 1 only:** the `Infrastructure -> EventBus` reference is
*not* removed yet (removing it now would break anyone relying on the current implicit
wiring). Phase 1 makes the new path available and functional, deprecates the old path,
and leaves both compiling. The reference removal itself is Phase 2 — a major-version
task, tracked separately, not executed here.

---

## 2. Task list (execute in order)

### Task 1 — Remove the dead `Domain -> Common` reference
**Risk: none. Non-breaking. Ship in this release without an Obsolete cycle.**

Nothing in `eBuildingBlocks.Domain` references any `eBuildingBlocks.Common` type
(verified: no hits for `GuidGenerator`, `MultiTenancyOptions`, `FeatureGate`,
`CustomClaimTypes` anywhere under `eBuildingBlocks.Domain/`).

- [ ] In `eBuildingBlocks.Domain/eBuildingBlocks.Domain.csproj`, delete:
  ```xml
  <ProjectReference Include="..\eBuildingBlocks.Common\eBuildingBlocks.Common.csproj" />
  ```
- [ ] Build `eBuildingBlocks.Domain` standalone (`dotnet build eBuildingBlocks.Domain`)
      and confirm it succeeds with zero errors.
- [ ] Build the full solution (`dotnet build eBuildingBlocks.sln`) to confirm no other
      project was implicitly relying on Domain re-exporting Common (it shouldn't be,
      since ProjectReferences aren't transitively re-exported by default, but verify).
- [ ] Bump `eBuildingBlocks.Domain` **patch** version (e.g. `3.0.0` -> `3.0.1`). This is
      not a breaking change — the public surface of `Domain` doesn't change, only an
      internal, unused build-time reference is removed.

---

### Task 2 — Add the broker-agnostic outbox publisher contract
**Risk: none. Purely additive.**

Add a new interface to `eBuildingBlocks.Common` (the one package every other package
already depends on, directly or indirectly) so both `Infrastructure` and `EventBus`
can depend *downward* on it without depending on each other.

- [ ] Create `eBuildingBlocks.Common/Outbox/IOutboxIntegrationPublisher.cs`:
  ```csharp
  namespace eBuildingBlocks.Common.Outbox;

  /// <summary>
  /// Broker-agnostic contract used by the transactional outbox processor to publish a
  /// dequeued domain event as an integration event. Implement this against whichever
  /// message bus you use (MassTransit, Azure Service Bus, Kafka, etc.) and register it
  /// in DI as <c>IOutboxIntegrationPublisher</c>.
  ///
  /// eBuildingBlocks.EventBus ships a ready-made MassTransit implementation — see
  /// <c>AddIntegrationMassTransit</c>, which registers it automatically.
  /// </summary>
  public interface IOutboxIntegrationPublisher
  {
      Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
  }
  ```
- [ ] Bump `eBuildingBlocks.Common` **minor** version (additive public API).

---

### Task 3 — Implement the contract in EventBus (additive, non-breaking)

- [ ] In `eBuildingBlocks.EventBus/Events/EventPublisher.cs`, add the new interface to
      the class declaration. No new methods are needed — `PublishAsync<T>` already
      matches the contract exactly:
  ```csharp
  using eBuildingBlocks.Common.Outbox;

  public sealed class EventPublisher(
      IPublishEndpoint publishEndpoint,
      IOptionsMonitor<MultiTenancyOptions> multiTenancyOptions)
      : IEventPublisher, IOutboxIntegrationPublisher
  {
      // existing members unchanged — PublishAsync<T> already satisfies IOutboxIntegrationPublisher
  }
  ```
- [ ] In `eBuildingBlocks.EventBus/Events/IntegrationMassTransitServiceCollectionExtensions.cs`,
      register the same instance under both interfaces inside `AddIntegrationMassTransit`:
  ```csharp
  services.AddScoped<IEventPublisher, EventPublisher>();
  services.AddScoped<IOutboxIntegrationPublisher>(sp => (IOutboxIntegrationPublisher)sp.GetRequiredService<IEventPublisher>());
  services.AddScoped<IEventSubscriber, EventSubscriber>();
  ```
  This means **any consumer already calling `AddIntegrationMassTransit` gets the new
  registration automatically, with zero code changes on their side.** This is the key
  move that makes Phase 1 fully backward-compatible.
- [ ] Bump `eBuildingBlocks.EventBus` **minor** version (additive).

---

### Task 4 — Make the outbox processor prefer the new contract, fall back to the old one, deprecate the old path

**File:** `eBuildingBlocks.Infrastructure/Outbox/OutboxProcessorBackgroundService.cs`

Current line (inside `ProcessOnceAsync`):
```csharp
var publisher = sp.GetRequiredService<IEventPublisher>();
```

Replace with a resolution helper that prefers the new contract and falls back to the
legacy one with a one-time deprecation warning:

- [ ] Add a private static flag + helper method to the class:
  ```csharp
  private static int _legacyPublisherWarningLogged;

  private static IOutboxIntegrationPublisher ResolvePublisher(IServiceProvider sp, ILogger logger)
  {
      var modern = sp.GetService<IOutboxIntegrationPublisher>();
      if (modern is not null)
          return modern;

      var legacy = sp.GetService<IEventPublisher>();
      if (legacy is not null)
      {
          if (Interlocked.Exchange(ref _legacyPublisherWarningLogged, 1) == 0)
          {
              logger.LogWarning(
                  "Outbox processor resolved IEventPublisher directly. This fallback is " +
                  "deprecated and will be removed in the next major version of " +
                  "eBuildingBlocks.Infrastructure. Register IOutboxIntegrationPublisher " +
                  "instead — if you already call AddIntegrationMassTransit(), this happens " +
                  "automatically as of eBuildingBlocks.EventBus 3.x. See the migration guide: " +
                  "https://github.com/ghazi1567/eBuildingBlocks/blob/main/docs/MIGRATION_OUTBOX_PUBLISHER.md");
          }
          return new LegacyEventPublisherAdapter(legacy);
      }

      throw new InvalidOperationException(
          "No outbox publisher registered. Register IOutboxIntegrationPublisher " +
          "(e.g. via eBuildingBlocks.EventBus's AddIntegrationMassTransit) before starting the outbox processor.");
  }
  ```
- [ ] Add a small private adapter in the same file (or a sibling file
      `LegacyEventPublisherAdapter.cs`) that wraps the old interface so the rest of the
      pipeline only ever talks to `IOutboxIntegrationPublisher`:
  ```csharp
  [Obsolete("Only used to bridge legacy IEventPublisher consumers. Will be removed alongside the legacy fallback.")]
  internal sealed class LegacyEventPublisherAdapter(IEventPublisher inner) : IOutboxIntegrationPublisher
  {
      public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
          => inner.PublishAsync(@event, cancellationToken);
  }
  ```
- [ ] Change the resolution line to:
  ```csharp
  var publisher = ResolvePublisher(sp, _logger);
  ```
- [ ] Update `OutboxPublisherDispatch.cs`'s `PublishAsync` signature to accept
      `IOutboxIntegrationPublisher` instead of `IEventPublisher` (mechanical rename —
      the generic method shape is identical, so this is a type swap only):
  ```csharp
  using eBuildingBlocks.Common.Outbox;
  // ...
  public static Task PublishAsync(IOutboxIntegrationPublisher publisher, object payload, CancellationToken cancellationToken)
  ```
  And update the reflection target inside `BuildInvoker` from
  `typeof(IEventPublisher)` to `typeof(IOutboxIntegrationPublisher)`.
- [ ] **Keep the existing `ProjectReference` to `eBuildingBlocks.EventBus` in
      `Infrastructure.csproj` for this release.** It's still needed for the fallback
      path (`IEventPublisher`) to compile. Removing it is Phase 2, done only after the
      deprecation window closes. Do **not** remove it as part of this task.
- [ ] Add `[Obsolete]` to the XML doc (not the compiler attribute, since it's a runtime
      DI concern, not a symbol consumers reference directly) on `OutboxProcessorBackgroundService`
      pointing at the new interface:
  ```csharp
  /// <summary>
  /// Polls <see cref="OutboxMessage"/> rows and publishes via <see cref="IOutboxIntegrationPublisher"/>
  /// (preferred) or, for backward compatibility, <c>IEventPublisher</c> from eBuildingBlocks.EventBus
  /// (deprecated — see migration guide linked in the runtime warning log).
  /// </summary>
  ```
- [ ] Bump `eBuildingBlocks.Infrastructure` **minor** version (fully additive/backward
      compatible — nothing existing stops compiling or behaving differently at runtime
      for current consumers).

---

### Task 5 — Write the developer migration guide

- [ ] Create `docs/MIGRATION_OUTBOX_PUBLISHER.md`:

  ```markdown
  # Migration: Outbox publisher resolution

  ## What changed
  `eBuildingBlocks.Infrastructure`'s outbox processor now prefers
  `eBuildingBlocks.Common.Outbox.IOutboxIntegrationPublisher` over the MassTransit-specific
  `IEventPublisher` from `eBuildingBlocks.EventBus`. This lets `Infrastructure` avoid a
  hard dependency on MassTransit/RabbitMQ for consumers who don't use the outbox +
  message broker combination.

  ## Do I need to do anything right now?
  **No.** If you currently call `services.AddIntegrationMassTransit(...)` from
  `eBuildingBlocks.EventBus`, the new interface is registered automatically as of
  `eBuildingBlocks.EventBus` 3.x. Your app keeps working with zero code changes.

  If you're on an older `eBuildingBlocks.EventBus` version that predates this change,
  the outbox processor still falls back to `IEventPublisher` automatically — you'll see
  a one-time warning in your logs, nothing breaks.

  ## What should I do to remove the warning / prepare for the next major version?
  1. Upgrade `eBuildingBlocks.EventBus` to >= 3.x (if not already).
  2. No further action needed — `AddIntegrationMassTransit` registers
     `IOutboxIntegrationPublisher` for you.

  ## If you're using a broker other than MassTransit, or a custom `IEventPublisher`
  Implement `IOutboxIntegrationPublisher` directly and register it:
  ```csharp
  services.AddScoped<IOutboxIntegrationPublisher, MyCustomOutboxPublisher>();
  ```

  ## Timeline
  - **Now (eBuildingBlocks.Infrastructure 3.x):** both paths work. Legacy path logs a
    warning once per process.
  - **Next major version (eBuildingBlocks.Infrastructure 4.0, separately scheduled):**
    the `IEventPublisher` fallback and the `EventBus` project reference are removed.
    Apps that have not registered `IOutboxIntegrationPublisher` by then will get a
    clear startup exception with the same guidance instead of a warning.
  ```

- [ ] Link this doc from `eBuildingBlocks.Infrastructure/README.md` and from the root
      `README.md`'s "Documentation" section.
- [ ] Add a `## Deprecations` section to `CHANGELOG.md` (create one at repo root if it
      doesn't exist yet) documenting the `IEventPublisher` fallback deprecation, the
      version it was introduced in, and the version it will be removed in.

---

### Task 6 — Tests

- [ ] Unit test: `OutboxProcessorBackgroundService` resolves `IOutboxIntegrationPublisher`
      when registered, and never touches `IEventPublisher` in that case.
- [ ] Unit test: when only `IEventPublisher` is registered (simulating an app on an
      older `EventBus` version or manual registration), the processor falls back
      correctly via `LegacyEventPublisherAdapter` and publishes successfully.
- [ ] Unit test: when neither is registered, `ResolvePublisher` throws
      `InvalidOperationException` with a message pointing at `IOutboxIntegrationPublisher`.
- [ ] Unit test: the one-time warning log fires exactly once across multiple
      `ProcessOnceAsync` iterations when using the legacy fallback (verifies the
      `Interlocked.Exchange` guard).
- [ ] Integration test (if the reference-app test harness supports it): run the
      existing `eBuildingBlocks.ReferenceApp.*` outbox flow end-to-end unchanged, and
      confirm it still publishes via MassTransit exactly as before — this is the
      regression check that proves zero behavior change for current consumers.
- [ ] Build the full solution and run the full test suite; all existing tests must
      pass unmodified (aside from the new ones above).

---

## 3. Explicit non-goals for this task (do not implement)

- Do **not** remove the `eBuildingBlocks.Infrastructure -> eBuildingBlocks.EventBus`
  `ProjectReference`. That's Phase 2, a major-version change, tracked and scheduled
  separately once the deprecation window has run its course.
- Do **not** remove `IEventPublisher`, `EventPublisher`, or any existing public type.
- Do **not** change the public signature of `AddOutboxProcessor<TDbContext>()` or any
  other existing DI extension method.
- Do **not** split `Infrastructure` into multiple NuGet packages in this task — that
  was considered and rejected in favor of the smaller interface-inversion approach,
  which achieves the same dependency-graph outcome (once Phase 2 lands) without adding
  a new package to version and publish.

---

## 4. Versioning summary for this task

| Package | Change type | Version bump | Breaking? |
|---|---|---|---|
| `eBuildingBlocks.Domain` | dead reference removed | patch | No |
| `eBuildingBlocks.Common` | new interface added | minor | No |
| `eBuildingBlocks.EventBus` | new interface implemented + auto-registered | minor | No |
| `eBuildingBlocks.Infrastructure` | prefers new interface, deprecates old, both work | minor | No |
| `eBuildingBlocks.Application` | no change | — | — |
| `eBuildingBlocks.API` | no change | — | — |
| `eBuildingBlocks.SMPP` | no change | — | — |

All existing production applications continue to build and run against these new
versions with no source or behavior changes required.

---

## 5. Phase 2 preview (not part of this task — for future planning only)

Once telemetry/adoption shows the legacy fallback is no longer hit in production
(e.g. the one-time warning has stopped appearing across consumer logs for a full
release cycle), a future major-version task will:

1. Remove `LegacyEventPublisherAdapter`, the `IEventPublisher` fallback branch in
   `ResolvePublisher`, and its `[Obsolete]` marker.
2. Remove the `ProjectReference` to `eBuildingBlocks.EventBus` from
   `Infrastructure.csproj` — this is the change that actually drops MassTransit/
   RabbitMQ.Client from `Infrastructure`'s transitive closure.
3. `ResolvePublisher` becomes a straight `sp.GetRequiredService<IOutboxIntegrationPublisher>()`
   call, throwing a clear startup exception if unregistered.
4. Bump `eBuildingBlocks.Infrastructure` to the next **major** version and publish the
   CHANGELOG breaking-change entry, pointing back at
   `docs/MIGRATION_OUTBOX_PUBLISHER.md`.

This is intentionally not scheduled here — it should only happen after real consumers
have had a full release cycle to pick up the Phase 1 changes passively.
