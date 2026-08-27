# Examples & Tutorials

Practical, copy-pasteable walkthroughs for common scenarios. For full API reference, see the deep-dive docs linked from each section — these tutorials are the fast path to a working setup; the linked docs cover every option and edge case.

| Tutorial | What you'll build |
|---|---|
| [Multi-tenant SaaS API in 10 minutes](multi-tenant-saas-quickstart.md) | A tenant-isolated Web API using `TenantEntity<TKey>`, `IUnitOfWork`, and automatic query filtering. |
| [Reliable event publishing with the outbox](reliable-events-with-outbox.md) | Domain events persisted in the same transaction as your data and published exactly-once via a background processor. |
| [Repository & unit of work](../REPOSITORY_AND_UOW.md) | `IUnitOfWork`, `IEfQueryableRepository`, and the specification pattern. |
| [Multi-tenancy deep dive](../MULTI_TENANCY.md) | Tenant resolution, header vs. JWT claims, worker/console hosts, bypassing filters safely. |
| [Transactional outbox deep dive](../HOSTING_APP_OUTBOX.md) | Full `IEventTypeRegistry`, interceptor, and SQL Server processor reference. |

## Scaffold a new project

Don't want to wire things up by hand? `dotnet new install eBuildingBlocks.Templates && dotnet new eblocks-api -n MyService` generates a complete, runnable Clean Architecture solution in one step — see [`templates/`](../../templates) for details.

## Reference apps

Rather than a snippet, [`eBuildingBlocks.ReferenceApp.API`](../../eBuildingBlocks.ReferenceApp.API) (plus its `.Domain`/`.Application`/`.Infrastructure` siblings) is a complete, runnable app wired up end-to-end — entities, repositories, controllers, outbox, multi-tenancy, and `Program.cs` all in one place. Run it with:

```bash
dotnet run --project ./eBuildingBlocks.ReferenceApp.API
```

[`eBuildingBlocks.API.Example`](../../eBuildingBlocks.API.Example) is a lighter-weight example focused specifically on the API layer (versioning, Scalar/OpenAPI, auth).

## Have a scenario you'd like documented?

Open an issue describing the use case — tutorials that come from real questions are more useful than ones we guess at.
