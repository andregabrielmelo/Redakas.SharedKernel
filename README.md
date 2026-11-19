# Redakas.SharedKernel

Base types shared by Clean Architecture projects, so a fix or a new building block is made once and reaches every project through a package update.

```bash
dotnet add package Redakas.SharedKernel
```

## What's in it

| Type | Purpose |
|---|---|
| `EntityBase`, `EntityBase<TId>`, `IAggregateRoot` | Entities with an id, and the marker for aggregate roots |
| `ValueObject` | Equality and ordering by components, for value objects that aren't records |
| `DomainEventBase`, `IDomainEvent`, `HasDomainEventsBase`, `IHasDomainEvents` | Domain events raised by entities (`RegisterDomainEvent`) |
| `IDomainEventHandler<T>` | A handler of one domain event (a Mediator notification handler) |
| `IDomainEventDispatcher`, `MediatorDomainEventDispatcher` | Publishes an entity's events through Mediator after a save and clears them. Best-effort: a failing handler is logged and never fails the caller; cancellation still stops it |
| `IRepository<T>`, `IReadRepository<T>` | Ardalis.Specification repositories over aggregate roots |
| `UniqueConstraintViolationException`, `ConcurrencyConflictException` | Persistence failures a use case can react to, without depending on the database provider |

It depends on [Ardalis.GuardClauses](https://github.com/ardalis/GuardClauses), [Ardalis.Specification](https://github.com/ardalis/Specification), [Mediator.Abstractions](https://github.com/martinothamar/Mediator) and `Microsoft.Extensions.Logging`. The consuming application adds Mediator's source generator and registers the dispatcher, for example `services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>()` next to an EF Core `SaveChangesInterceptor` that calls it.

## Changing it

1. Change the code here, with tests, in a pull request into `main`. CI checks formatting (CSharpier), builds with the analyzers as errors, tests and packs.
2. Release it by tagging the merge commit:
   ```bash
   git tag v1.2.0
   git push origin v1.2.0
   ```
   The `Release` workflow packs that version (MinVer reads it from the tag) and pushes it, with its symbols, to nuget.org.
3. Each project picks it up through Dependabot's weekly NuGet pull request, or at once with `dotnet add package Redakas.SharedKernel`.

Use [semantic versioning](https://semver.org/): a breaking change to a public type is a new major version.

### Trying a change before releasing it

```bash
dotnet pack src/Redakas.SharedKernel -c Release -o ~/nuget-local
```

Then, in the consuming project, add the folder as a package source (`dotnet nuget add source ~/nuget-local --name local`) and reference the prerelease version the pack printed (untagged commits build as prereleases, such as `1.2.1-alpha.0.3`). Remove the source again before committing.

## Development

```bash
dotnet tool restore
dotnet csharpier check .
dotnet build Redakas.SharedKernel.slnx -c Release
dotnet test Redakas.SharedKernel.slnx -c Release
```

The .NET SDK version is pinned in `global.json`. After changing a package version, run `dotnet restore` and commit the regenerated `packages.lock.json` files.

## License

MIT
