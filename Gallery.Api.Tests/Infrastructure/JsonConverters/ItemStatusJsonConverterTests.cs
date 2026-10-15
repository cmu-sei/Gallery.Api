// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Text.Json;
using Gallery.Api.Data.Enumerations;
using Gallery.Api.Infrastructure.JsonConverters;

namespace Gallery.Api.Tests.Infrastructure.JsonConverters;

/// <summary>
/// <c>ItemStatusJsonConverter</c>, which MVC, the SignalR protocol and the seed file use for an article's
/// status: it writes the color name the wall paints, and reads the color names, the names used before the
/// statuses were named for colors, and the stored numbers.
/// </summary>
public class ItemStatusJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new ItemStatusJsonConverter() } };

    private static ItemStatus Read(string json) => JsonSerializer.Deserialize<ItemStatus>(json, Options);

    // ---- Write ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(ItemStatus.Gray, "\"Gray\"")]
    [InlineData(ItemStatus.Red, "\"Red\"")]
    [InlineData(ItemStatus.Orange, "\"Orange\"")]
    [InlineData(ItemStatus.Yellow, "\"Yellow\"")]
    [InlineData(ItemStatus.Green, "\"Green\"")]
    public void Write_writes_each_status_as_its_color_name(ItemStatus status, string expected)
    {
        Assert.Equal(expected, JsonSerializer.Serialize(status, Options));
    }

    /// <summary>A value no member names is written as its number, in quotes.</summary>
    [Fact]
    public void Write_of_a_value_outside_the_defined_ones_writes_its_number_as_a_string()
    {
        Assert.Equal("\"30\"", JsonSerializer.Serialize((ItemStatus)30, Options));
    }

    // ---- Read: names ---------------------------------------------------------------------------

    [Theory]
    [InlineData("\"Gray\"", ItemStatus.Gray)]
    [InlineData("\"Red\"", ItemStatus.Red)]
    [InlineData("\"Orange\"", ItemStatus.Orange)]
    [InlineData("\"Yellow\"", ItemStatus.Yellow)]
    [InlineData("\"Green\"", ItemStatus.Green)]
    public void Read_reads_each_color_name(string json, ItemStatus expected)
    {
        Assert.Equal(expected, Read(json));
    }

    [Theory]
    [InlineData("\"red\"", ItemStatus.Red)]
    [InlineData("\"GREEN\"", ItemStatus.Green)]
    [InlineData("\" Yellow \"", ItemStatus.Yellow)]
    public void Read_reads_a_color_name_in_any_case_and_with_surrounding_spaces(string json, ItemStatus expected)
    {
        Assert.Equal(expected, Read(json));
    }

    /// <summary>Clients and collection files written before the rename keep their meaning.</summary>
    [Theory]
    [InlineData("\"Unused\"", ItemStatus.Gray)]
    [InlineData("\"Closed\"", ItemStatus.Red)]
    [InlineData("\"Critical\"", ItemStatus.Orange)]
    [InlineData("\"Affected\"", ItemStatus.Yellow)]
    [InlineData("\"Open\"", ItemStatus.Green)]
    public void Read_reads_each_legacy_name_as_its_color(string json, ItemStatus expected)
    {
        Assert.Equal(expected, Read(json));
    }

    [Theory]
    [InlineData("\"closed\"", ItemStatus.Red)]
    [InlineData("\"OPEN\"", ItemStatus.Green)]
    [InlineData("\" Critical \"", ItemStatus.Orange)]
    public void Read_reads_a_legacy_name_in_any_case_and_with_surrounding_spaces(string json, ItemStatus expected)
    {
        Assert.Equal(expected, Read(json));
    }

    [Theory]
    [InlineData("\"Purple\"", "'Purple' is not a valid ItemStatus value.")]
    [InlineData("\"Resolved\"", "'Resolved' is not a valid ItemStatus value.")]
    [InlineData("\"\"", "'' is not a valid ItemStatus value.")]
    [InlineData("\"  \"", "'  ' is not a valid ItemStatus value.")]
    public void Read_refuses_a_name_that_is_neither_a_color_nor_a_legacy_name(string json, string message)
    {
        Assert.Equal(message, Assert.Throws<JsonException>(() => Read(json)).Message);
    }

    /// <summary>A comma-separated pair of color names reads as the two values combined, which no member names.</summary>
    // Same case as ArticleControllerTests.Create_stores_a_comma_separated_pair_of_status_names_as_an_undefined_status.
    [Theory]
    [InlineData("\"Red, Orange\"", 30)]
    [InlineData("\"Orange,Yellow\"", 60)]
    public void Read_of_a_comma_separated_pair_of_color_names_returns_a_value_no_member_names(string json, int expected)
    {
        Assert.Equal(expected, (int)Read(json));
    }

    // ---- Read: numbers -------------------------------------------------------------------------

    [Theory]
    [InlineData("0", ItemStatus.Gray)]
    [InlineData("10", ItemStatus.Red)]
    [InlineData("20", ItemStatus.Orange)]
    [InlineData("40", ItemStatus.Yellow)]
    [InlineData("50", ItemStatus.Green)]
    public void Read_reads_each_defined_number(string json, ItemStatus expected)
    {
        Assert.Equal(expected, Read(json));
    }

    [Theory]
    [InlineData("\"0\"", ItemStatus.Gray)]
    [InlineData("\"10\"", ItemStatus.Red)]
    [InlineData("\"20\"", ItemStatus.Orange)]
    [InlineData("\"40\"", ItemStatus.Yellow)]
    [InlineData("\" 50 \"", ItemStatus.Green)]
    public void Read_reads_each_defined_number_in_quotes(string json, ItemStatus expected)
    {
        Assert.Equal(expected, Read(json));
    }

    [Theory]
    [InlineData("30", "30 is not a valid ItemStatus value.")]
    [InlineData("999", "999 is not a valid ItemStatus value.")]
    [InlineData("-10", "-10 is not a valid ItemStatus value.")]
    public void Read_refuses_a_number_outside_the_defined_ones(string json, string message)
    {
        Assert.Equal(message, Assert.Throws<JsonException>(() => Read(json)).Message);
    }

    [Theory]
    [InlineData("\"30\"", "'30' is not a valid ItemStatus value.")]
    [InlineData("\"-10\"", "'-10' is not a valid ItemStatus value.")]
    public void Read_refuses_a_quoted_number_outside_the_defined_ones(string json, string message)
    {
        Assert.Equal(message, Assert.Throws<JsonException>(() => Read(json)).Message);
    }

    [Theory]
    [InlineData("10.5")]
    [InlineData("3000000000")]
    public void Read_refuses_a_number_that_is_not_an_integer_status(string json)
    {
        Assert.Throws<JsonException>(() => Read(json));
    }

    // ---- Read: other tokens --------------------------------------------------------------------

    [Fact]
    public void Read_refuses_a_null_status()
    {
        Assert.Equal("'' is not a valid ItemStatus value.", Assert.Throws<JsonException>(() => Read("null")).Message);
    }

    [Fact]
    public void Read_refuses_a_boolean()
    {
        Assert.Throws<JsonException>(() => Read("true"));
    }

    /// <summary>An optional status reads null as null and goes through the converter otherwise.</summary>
    [Theory]
    [InlineData("null", null)]
    [InlineData("\"Closed\"", ItemStatus.Red)]
    [InlineData("\"Orange\"", ItemStatus.Orange)]
    public void Read_of_an_optional_status_uses_the_converter(string json, ItemStatus? expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<ItemStatus?>(json, Options));
    }

    // ---- TryParse ------------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Purple")]
    [InlineData("30")]
    public void TryParse_is_false_for_a_value_that_names_no_status(string value)
    {
        Assert.False(ItemStatusJsonConverter.TryParse(value, out _));
    }

    [Theory]
    [InlineData("Orange", ItemStatus.Orange)]
    [InlineData("Critical", ItemStatus.Orange)]
    [InlineData("20", ItemStatus.Orange)]
    public void TryParse_reads_a_color_name_a_legacy_name_or_a_number(string value, ItemStatus expected)
    {
        Assert.True(ItemStatusJsonConverter.TryParse(value, out var status));
        Assert.Equal(expected, status);
    }
}
