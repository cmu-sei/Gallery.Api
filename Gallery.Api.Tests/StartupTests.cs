// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Text.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Tests.Support;
using Gallery.Api.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Gallery.Api.Tests;

/// <summary>
/// The JSON options <c>Startup.ConfigureServices</c> gives the SignalR protocol, which carries the entity
/// handlers' article broadcasts to the wall.
/// </summary>
/// <remarks>
/// The run-wide host replaces the hub contexts with recorders, which keep the payload objects, and no hub
/// method sends an article to its caller, so no connection ever receives one. The protocol's own options
/// are read from the host instead, and an article is written and read with them as the protocol does.
/// </remarks>
public class StartupTests(GalleryAppFactory factory)
{
    private JsonSerializerOptions HubPayloadOptions =>
        factory.Services.GetRequiredService<IOptions<JsonHubProtocolOptions>>().Value.PayloadSerializerOptions;

    [Fact]
    public void The_hub_protocol_writes_an_article_s_status_as_its_color_name()
    {
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(new Article { Name = "Broadcast", Status = ItemStatus.Orange }, HubPayloadOptions));

        var status = payload.RootElement.GetProperty("status");
        Assert.Equal((JsonValueKind.String, "Orange"), (status.ValueKind, status.ToString()));
    }

    /// <summary>The status converter comes ahead of the enum converter, so the legacy names still read.</summary>
    [Theory]
    [InlineData("{\"status\":\"Closed\"}", ItemStatus.Red)]
    [InlineData("{\"status\":\"Yellow\"}", ItemStatus.Yellow)]
    [InlineData("{\"status\":20}", ItemStatus.Orange)]
    public void The_hub_protocol_reads_a_color_name_a_legacy_name_or_a_number(string json, ItemStatus expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<Article>(json, HubPayloadOptions).Status);
    }

    [Fact]
    public void The_hub_protocol_refuses_a_status_no_member_names()
    {
        var thrown = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Article>("{\"status\":\"Purple\"}", HubPayloadOptions));

        Assert.Equal("'Purple' is not a valid ItemStatus value.", thrown.Message);
    }
}
