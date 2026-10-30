// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Data.Models;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;

namespace Gallery.Api.Tests.Infrastructure.Mappings;

/// <summary>The real AutoMapper profiles with Startup's global convention (<see cref="TestMapper"/>).</summary>
public class MappingConfigurationTests
{
    /// <summary>An edit body has no id, collection or exhibit, so mapping one keeps the entity's.</summary>
    [Fact]
    public void An_article_edit_keeps_the_entity_s_id_collection_and_exhibit()
    {
        var entity = new ArticleEntity { Id = Guid.NewGuid(), CollectionId = Guid.NewGuid(), ExhibitId = Guid.NewGuid() };
        var (id, collectionId, exhibitId) = (entity.Id, entity.CollectionId, entity.ExhibitId);

        TestMapper.Mapper.Map(new ArticleEdit { Name = "Edited" }, entity);

        Assert.Equal(("Edited", id, collectionId, exhibitId), (entity.Name, entity.Id, entity.CollectionId, entity.ExhibitId));
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
