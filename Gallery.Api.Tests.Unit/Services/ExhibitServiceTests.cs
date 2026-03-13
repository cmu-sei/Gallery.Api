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
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.Services;
using Gallery.Api.Tests.Shared.Fixtures;
using Gallery.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Gallery.Api.Tests.Unit.Services;

public class ExhibitServiceTests
{
    private readonly IFixture _fixture;
    private readonly GalleryDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMapper _mapper;
    private readonly IUserArticleService _userArticleService;
    private readonly IUserClaimsService _userClaimsService;
    private readonly ClaimsPrincipal _user;
    private readonly Guid _userId;
    private readonly ExhibitService _sut;

    public ExhibitServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new GalleryCustomization());

        _context = TestDbContextFactory.Create<GalleryDbContext>();

        _authorizationService = A.Fake<IAuthorizationService>();
        _mapper = A.Fake<IMapper>();
        _userArticleService = A.Fake<IUserArticleService>();
        _userClaimsService = A.Fake<IUserClaimsService>();

        _userId = Guid.NewGuid();
        _user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", _userId.ToString())
        }, "TestAuth"));

        _sut = new ExhibitService(
            _context,
            _authorizationService,
            _user,
            _mapper,
            _userArticleService,
            _userClaimsService);
    }

    [Fact]
    public async Task GetAsync_WhenCanViewAll_ReturnsAllExhibits()
    {
        // Arrange
        var collection = _fixture.Create<CollectionEntity>();
        await _context.Collections.AddAsync(collection);

        var exhibits = _fixture.Build<ExhibitEntity>()
            .With(x => x.CollectionId, collection.Id)
            .Without(x => x.Collection)
            .Without(x => x.Teams)
            .Without(x => x.Memberships)
            .CreateMany(3).ToList();
        await _context.Exhibits.AddRangeAsync(exhibits);
        await _context.SaveChangesAsync();

        var expected = exhibits.Select(e => new ViewModels.Exhibit { Id = e.Id }).ToList();
        A.CallTo(() => _mapper.Map<IEnumerable<ViewModels.Exhibit>>(A<object>._))
            .Returns(expected);

        // Act
        var result = await _sut.GetAsync(true, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsExhibit()
    {
        // Arrange
        var collection = _fixture.Create<CollectionEntity>();
        await _context.Collections.AddAsync(collection);

        var entity = _fixture.Build<ExhibitEntity>()
            .With(x => x.CollectionId, collection.Id)
            .Without(x => x.Collection)
            .Without(x => x.Teams)
            .Without(x => x.Memberships)
            .Create();
        await _context.Exhibits.AddAsync(entity);
        await _context.SaveChangesAsync();

        var expected = new ViewModels.Exhibit { Id = entity.Id, Name = entity.Name };
        A.CallTo(() => _mapper.Map<ViewModels.Exhibit>(A<ExhibitEntity>._))
            .Returns(expected);

        // Act
        var result = await _sut.GetAsync(entity.Id, false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }

    [Fact]
    public async Task CreateAsync_WithMissingCollectionId_ThrowsArgumentException()
    {
        // Arrange
        var exhibit = new ViewModels.Exhibit { CollectionId = Guid.Empty };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateAsync(exhibit, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentCollection_ThrowsEntityNotFoundException()
    {
        // Arrange
        var exhibit = new ViewModels.Exhibit { CollectionId = Guid.NewGuid() };

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<ViewModels.Collection>>(
            () => _sut.CreateAsync(exhibit, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenExhibitExists_ReturnsTrue()
    {
        // Arrange
        var collection = _fixture.Create<CollectionEntity>();
        await _context.Collections.AddAsync(collection);

        var entity = _fixture.Build<ExhibitEntity>()
            .With(x => x.CollectionId, collection.Id)
            .Without(x => x.Collection)
            .Without(x => x.Teams)
            .Without(x => x.Memberships)
            .Create();
        await _context.Exhibits.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(entity.Id, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Exhibits.FindAsync(entity.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenExhibitDoesNotExist_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<ViewModels.Exhibit>>(
            () => _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenExhibitDoesNotExist_ThrowsEntityNotFoundException()
    {
        // Arrange
        var exhibit = _fixture.Create<ViewModels.Exhibit>();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<ViewModels.Exhibit>>(
            () => _sut.UpdateAsync(Guid.NewGuid(), exhibit, CancellationToken.None));
    }
}
