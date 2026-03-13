// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using System.Security.Principal;
using AutoFixture;
using AutoMapper;
using Crucible.Common.Testing.Fixtures;
using FakeItEasy;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Services;
using Gallery.Api.Tests.Shared.Fixtures;
using Gallery.Api.ViewModels;
using Xunit;

namespace Gallery.Api.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class CollectionServiceTests
{
    private static (GalleryDbContext context, CollectionService sut, IMapper mapper, IFixture fixture) CreateTestContext()
    {
        var fixture = new Fixture();
        fixture.Customize(new GalleryCustomization());

        var context = TestDbContextFactory.Create<GalleryDbContext>();
        var userClaimsService = A.Fake<IUserClaimsService>();
        var mapper = A.Fake<IMapper>();

        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString())
        }, "TestAuth"));

        var sut = new CollectionService(context, userClaimsService, user, mapper);
        return (context, sut, mapper, fixture);
    }

    [Fact]
    public async Task GetAsync_WhenCanViewAll_ReturnsAllCollections()
    {
        var (context, sut, mapper, fixture) = CreateTestContext();

        var entities = Enumerable.Range(0, 3).Select(_ => new CollectionEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Collection-{Guid.NewGuid()}",
            Description = "Test"
        }).ToList();
        await context.Collections.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        var expected = entities.Select(e => new ViewModels.Collection { Id = e.Id, Name = e.Name }).ToList();
        A.CallTo(() => mapper.Map<IEnumerable<ViewModels.Collection>>(A<object>._))
            .Returns(expected);

        var result = await sut.GetAsync(true, CancellationToken.None);

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsCollection()
    {
        var (context, sut, mapper, fixture) = CreateTestContext();

        var entity = fixture.Create<CollectionEntity>();
        await context.Collections.AddAsync(entity);
        await context.SaveChangesAsync();

        var expected = new ViewModels.Collection { Id = entity.Id, Name = entity.Name };
        A.CallTo(() => mapper.Map<ViewModels.Collection>(A<CollectionEntity>._))
            .Returns(expected);

        var result = await sut.GetAsync(entity.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }

    [Fact]
    public async Task DeleteAsync_WhenCollectionExists_ReturnsTrue()
    {
        var (context, sut, _, fixture) = CreateTestContext();

        var entity = fixture.Create<CollectionEntity>();
        await context.Collections.AddAsync(entity);
        await context.SaveChangesAsync();

        var result = await sut.DeleteAsync(entity.Id, CancellationToken.None);

        Assert.True(result);
        Assert.Null(await context.Collections.FindAsync(entity.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenCollectionDoesNotExist_ThrowsEntityNotFoundException()
    {
        var (_, sut, _, _) = CreateTestContext();

        await Assert.ThrowsAsync<Gallery.Api.Infrastructure.Exceptions.EntityNotFoundException<ViewModels.Collection>>(
            () => sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenCollectionDoesNotExist_ThrowsEntityNotFoundException()
    {
        var (_, sut, _, fixture) = CreateTestContext();

        var collection = fixture.Create<ViewModels.Collection>();

        await Assert.ThrowsAsync<Gallery.Api.Infrastructure.Exceptions.EntityNotFoundException<ViewModels.Collection>>(
            () => sut.UpdateAsync(Guid.NewGuid(), collection, CancellationToken.None));
    }
}
