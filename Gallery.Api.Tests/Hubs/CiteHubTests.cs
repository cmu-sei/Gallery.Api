// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gallery.Api.Hubs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gallery.Api.Tests.Hubs;

/// <summary><see cref="CiteHub"/>: CITE's connection joins the caller's personal <c>-cite</c> group, where unread counts go.</summary>
public class CiteHubTests
{
    [Fact]
    public async Task Join_adds_the_connection_to_the_caller_s_cite_group()
    {
        var harness = new HubHarness();
        var hub = harness.Attach(new CiteHub(NullLogger<CiteHub>.Instance));

        await hub.Join();

        await harness.Groups.Received(1).AddToGroupAsync(
            HubHarness.ConnectionId, $"{harness.UserId}{CiteHubMethods.GroupNameSuffix}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leave_removes_the_connection_from_the_caller_s_cite_group()
    {
        var harness = new HubHarness();
        var hub = harness.Attach(new CiteHub(NullLogger<CiteHub>.Instance));

        await hub.Leave();

        await harness.Groups.Received(1).RemoveFromGroupAsync(
            HubHarness.ConnectionId, $"{harness.UserId}{CiteHubMethods.GroupNameSuffix}", Arg.Any<CancellationToken>());
    }
}
