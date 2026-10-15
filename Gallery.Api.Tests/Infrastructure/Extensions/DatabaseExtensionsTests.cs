// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.Extensions;
using Gallery.Api.Infrastructure.Options;
using Gallery.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gallery.Api.Tests.Infrastructure.Extensions;

/// <summary>
/// <c>DatabaseExtensions.InitializeDatabase</c> with a seed file (<c>Database:SeedFile</c>), which it reads
/// with the status converter ahead of the enum converter: a seeded article's status may be a color name, a
/// legacy name or a number.
/// </summary>
/// <remarks>
/// The run-wide host's own startup seeds from appsettings, not a file, so these tests build a bare host
/// over the test's own database, holding only what <c>InitializeDatabase</c> resolves, and call it.
/// </remarks>
public class DatabaseExtensionsTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    private const string SeedFileName = "seed.json";

    [Theory]
    [InlineData("\"Orange\"", ItemStatus.Orange)]
    [InlineData("\"Closed\"", ItemStatus.Red)]
    [InlineData("\"unused\"", ItemStatus.Gray)]
    [InlineData("40", ItemStatus.Yellow)]
    [InlineData("\"50\"", ItemStatus.Green)]
    public async Task InitializeDatabase_seeds_an_article_status_given_by_color_legacy_name_or_number(string status, ItemStatus expected)
    {
        var articleId = Guid.NewGuid();
        using var seed = new SeedDirectory(SeedFile(articleId, status));
        using var host = HostOver(seed.Path);

        host.InitializeDatabase();

        Assert.Equal(expected, await ReadBack(db => db.Articles.Where(x => x.Id == articleId).Select(x => x.Status).SingleAsync(Ct)));
    }

    /// <summary>A seed file naming a status no member names stops startup before anything is seeded.</summary>
    [Theory]
    [InlineData("\"Purple\"", "'Purple' is not a valid ItemStatus value.")]
    [InlineData("30", "30 is not a valid ItemStatus value.")]
    public async Task InitializeDatabase_throws_on_a_seed_file_status_no_member_names_and_seeds_nothing(string status, string message)
    {
        using var seed = new SeedDirectory(SeedFile(Guid.NewGuid(), status));
        using var host = HostOver(seed.Path);

        var thrown = Assert.Throws<JsonException>(() => host.InitializeDatabase());

        Assert.Equal(message, thrown.Message);
        Assert.False(await ReadBack(db => db.Collections.AnyAsync(Ct)));
    }

    /// <summary>A host with the services <c>InitializeDatabase</c> resolves: the database options, a context over this test's database, logging, and the environment whose content root holds the seed file.</summary>
    private IHost HostOver(string contentRoot) =>
        new HostBuilder()
            .UseContentRoot(contentRoot)
            .ConfigureServices(services => services
                .AddLogging()
                .AddSingleton(new DatabaseOptions { SeedFile = SeedFileName })
                .AddScoped(_ => NewContext()))
            .Build();

    /// <summary>A seed file holding one collection and one article on it, whose status is the raw JSON given.</summary>
    private static string SeedFile(Guid articleId, string status)
    {
        var collectionId = Guid.NewGuid();

        return new JsonObject
        {
            ["Collections"] = new JsonArray(new JsonObject { ["Id"] = collectionId, ["Name"] = "Seeded Collection" }),
            ["Articles"] = new JsonArray(new JsonObject
            {
                ["Id"] = articleId,
                ["CollectionId"] = collectionId,
                ["Name"] = "Seeded Article",
                ["SourceType"] = "News",
                ["Status"] = JsonNode.Parse(status),
                ["DatePosted"] = TestData.DefaultDatePosted
            })
        }.ToJsonString();
    }

    /// <summary>A temporary content root holding the seed file, deleted on dispose.</summary>
    private sealed class SeedDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("gallery-seed-");

        public SeedDirectory(string seedFile) => File.WriteAllText(System.IO.Path.Combine(_directory.FullName, SeedFileName), seedFile);

        public string Path => _directory.FullName;

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
