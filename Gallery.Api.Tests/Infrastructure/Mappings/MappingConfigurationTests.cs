// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.Mappings;

/// <summary>The real AutoMapper profiles with Startup's global convention (<see cref="TestMapper"/>).</summary>
public class MappingConfigurationTests
{
    /// <summary>A view model's id maps onto the entity, so an update body names the key it rewrites.</summary>
    [Fact]
    public void An_article_body_maps_its_id_onto_the_entity()
    {
        var id = Guid.NewGuid();
        var entity = new ArticleEntity { Id = Guid.NewGuid() };

        TestMapper.Mapper.Map(new Article { Id = id }, entity);

        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void A_team_maps_its_users_through_its_team_users()
    {
        var user = TestData.User(name: "On The Team");
        var team = new TeamEntity { Id = Guid.NewGuid(), TeamUsers = [new TeamUserEntity { User = user }] };

        var mapped = TestMapper.Mapper.Map<Team>(team);

        Assert.Equal("On The Team", Assert.Single(mapped.Users).Name);
    }
}
