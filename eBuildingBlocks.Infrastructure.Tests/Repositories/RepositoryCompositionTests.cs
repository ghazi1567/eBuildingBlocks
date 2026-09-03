using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Implementations;
using Xunit;

namespace eBuildingBlocks.Infrastructure.Tests.Repositories;

/// <summary>
/// Regression coverage for TODO.md P-3: <see cref="Repository{TEntity,TKey,TDbContext}"/> used to
/// inherit <see cref="UnitOfWork{TDbContext}"/> (an IS-A relationship that isn't true — a repository
/// is not a unit of work), which put <c>SaveChangesAsync</c>/<c>BeginTransactionAsync</c>/
/// <c>ExecuteSqlAsync</c> on every repository's public surface. It now composes the DbContext
/// directly instead.
/// </summary>
public class RepositoryCompositionTests
{
    [Fact]
    public void Repository_DoesNotImplement_IUnitOfWork()
    {
        var repositoryType = typeof(Repository<,,>);

        Assert.False(typeof(IUnitOfWork).IsAssignableFrom(repositoryType));
    }

    [Fact]
    public void Repository_DoesNotInheritFrom_UnitOfWork()
    {
        var repositoryType = typeof(Repository<,,>);
        var unitOfWorkType = typeof(UnitOfWork<>);

        for (var t = repositoryType.BaseType; t is not null; t = t.BaseType)
        {
            var current = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
            Assert.NotEqual(unitOfWorkType, current);
        }
    }

    [Theory]
    [InlineData("SaveChangesAsync")]
    [InlineData("BeginTransactionAsync")]
    [InlineData("ExecuteSqlAsync")]
    [InlineData("Entities")]
    public void Repository_DoesNotExposeUnitOfWorkMembers(string memberName)
    {
        var repositoryType = typeof(Repository<,,>);

        var publicMember = repositoryType
            .GetMethods()
            .FirstOrDefault(m => m.Name == memberName && m.IsPublic);

        Assert.Null(publicMember);
    }
}
