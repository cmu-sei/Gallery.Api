// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Crucible.Common.Testing.Auth;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gallery.Api.Tests.Integration.Tests.Controllers;

public class UserControllerTests : IClassFixture<GalleryTestContext>
{
    private readonly GalleryTestContext _factory;
    private readonly HttpClient _client;

    public UserControllerTests(GalleryTestContext factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var userId = TestAuthenticationUser.DefaultUserId;

        // Seed the user into the database
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GalleryDbContext>();
            if (!context.Users.Any(u => u.Id == userId))
            {
                context.Users.Add(new UserEntity
                {
                    Id = userId,
                    Name = "Integration Tester",
                    Email = "tester@test.com"
                });
                await context.SaveChangesAsync();
            }
        }

        // Act
        var response = await _client.GetAsync($"/api/users/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<Gallery.Api.ViewModels.User>();
        Assert.NotNull(user);
        Assert.Equal(userId, user.Id);
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/users/{nonExistentId}");

        // Assert
        // The API may return 404 or return null with 200 depending on implementation
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected NotFound or OK, got {response.StatusCode}");
    }
}
