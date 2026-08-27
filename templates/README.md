# eBuildingBlocks.Templates

`dotnet new` templates for scaffolding eBuildingBlocks-based projects, packaged as the `eBuildingBlocks.Templates` NuGet package.

## For users

```bash
dotnet new install eBuildingBlocks.Templates
dotnet new eblocks-api -n MyService
cd MyService
dotnet run --project MyService.API
```

This scaffolds a working Clean Architecture Web API (Domain/Application/Infrastructure/API projects) wired to the eBuildingBlocks packages, using an EF Core in-memory database so it runs with zero external services. See the generated project's own `README.md` for what's included and how to move to a real database, multi-tenancy, or reliable event publishing.

## For contributors

The template source lives under `content/eBuildingBlocks.CleanArchitecture/`. It's a normal, buildable multi-project solution — `eBlocksApi` is the placeholder name (`sourceName` in `.template.config/template.json`) that `dotnet new -n <name>` replaces across file/folder names and file contents.

To test changes locally without publishing to NuGet:

```bash
dotnet new install ./templates/content/eBuildingBlocks.CleanArchitecture
dotnet new eblocks-api -n TestProject -o /tmp/TestProject
cd /tmp/TestProject
dotnet build
dotnet run --project TestProject.API

# Uninstall when done testing:
dotnet new uninstall ./templates/content/eBuildingBlocks.CleanArchitecture
```

The `eBuildingBlocks.Templates.csproj` at the root of this folder is only the NuGet packaging wrapper (`dotnet pack`) — you don't need to build it to test the template itself; `dotnet new install` against the content folder is faster for iteration.

Keep the template's `eBuildingBlocks.*` package versions (in the generated `.csproj` files) reasonably in sync with the versions published from this repo — a stale template that references a version with a since-fixed bug is a bad first impression.
