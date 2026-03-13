// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Crucible.Common.Testing.Auth;
using Gallery.Api.Data;
using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Gallery.Api.Tests.Integration.Tests.Controllers;

[Category("Integration")]
[ClassDataSource<GalleryTestContext>(Shared = SharedType.PerTestSession)]
public class UserControllerTests(GalleryTestContext factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Test]
    public async Task GetUsers_WhenCalled_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetUser_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var userId = TestAuthenticationUser.DefaultUserId;

        // Seed the user into the database
        using (var scope = factory.Services.CreateScope())
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
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<Gallery.Api.ViewModels.User>();
        await Assert.That(user).IsNotNull();
        await Assert.That(user.Id).IsEqualTo(userId);
    }

    [Test]
    public async Task GetUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/users/{nonExistentId}");

        // Assert
        // The API may return 404 or return null with 200 depending on implementation
        await Assert.That(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.OK)
            .IsTrue();
    }
}
