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
using Microsoft.Extensions.Logging;
using Xunit;

namespace Gallery.Api.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class UserServiceTests
{
    private readonly IFixture _fixture;
    private readonly GalleryDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IMapper _mapper;
    private readonly ILogger<ITeamUserService> _logger;
    private readonly ClaimsPrincipal _user;
    private readonly Guid _userId;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new GalleryCustomization());

        _context = TestDbContextFactory.Create<GalleryDbContext>();

        _authorizationService = A.Fake<IAuthorizationService>();
        _mapper = A.Fake<IMapper>();
        _logger = A.Fake<ILogger<ITeamUserService>>();

        _userId = Guid.NewGuid();
        _user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", _userId.ToString())
        }, "TestAuth"));

        _sut = new UserService(_context, _user, _authorizationService, _logger, _mapper);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserDoesNotExist_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<ViewModels.User>>(
            () => _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenDeletingSelf_ThrowsForbiddenException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.DeleteAsync(_userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_ReturnsTrue()
    {
        // Arrange
        var entity = _fixture.Build<UserEntity>()
            .With(x => x.Id, Guid.NewGuid())
            .Without(x => x.TeamUsers)
            .Without(x => x.Role)
            .Without(x => x.ExhibitMemberships)
            .Without(x => x.CollectionMemberships)
            .Without(x => x.GroupMemberships)
            .Create();
        await _context.Users.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(entity.Id, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Users.FindAsync(entity.Id));
    }

    [Fact]
    public async Task UpdateAsync_WhenIdMismatch_ThrowsForbiddenException()
    {
        // Arrange
        var user = new ViewModels.User { Id = Guid.NewGuid() };

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), user, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenUserDoesNotExist_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ViewModels.User { Id = userId };

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<ViewModels.User>>(
            () => _sut.UpdateAsync(userId, user, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithValidUser_AddsUserToContext()
    {
        // This test needs a real mapper because CreateAsync internally calls GetAsync
        // which uses ProjectTo<User>() requiring a real IConfigurationProvider
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<Gallery.Api.Infrastructure.Mappings.UserProfile>());
        var realMapper = config.CreateMapper();

        var context = TestDbContextFactory.Create<GalleryDbContext>();
        var sut = new UserService(context, _user, _authorizationService, _logger, realMapper);

        var userVm = new ViewModels.User { Id = Guid.NewGuid(), Name = "Test User" };

        // Act
        var result = await sut.CreateAsync(userVm, CancellationToken.None);

        // Assert
        var saved = await context.Users.FindAsync(userVm.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test User", saved.Name);
    }
}
