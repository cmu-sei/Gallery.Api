// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gallery.Api.Data.Enumerations;

namespace Gallery.Api.Infrastructure.JsonConverters
{
    // writes the color name, and still reads the names used before the statuses were named for colors,
    // so existing clients, MSELs and seed files keep working.
    // must be added ahead of JsonStringEnumConverter, which would otherwise claim ItemStatus
    public class ItemStatusJsonConverter : JsonConverter<ItemStatus>
    {
        private static readonly Dictionary<string, ItemStatus> LegacyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Unused", ItemStatus.Gray },
            { "Open", ItemStatus.Green },
            { "Affected", ItemStatus.Yellow },
            { "Critical", ItemStatus.Orange },
            { "Closed", ItemStatus.Red }
        };

        public static bool TryParse(string value, out ItemStatus status)
        {
            value = value?.Trim();
            status = default;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            if (int.TryParse(value, out var number))
            {
                // a quoted number, which JsonStringEnumConverter accepted too
                status = (ItemStatus)number;
                return Enum.IsDefined(typeof(ItemStatus), number);
            }

            return Enum.TryParse(value, true, out status) || LegacyNames.TryGetValue(value, out status);
        }

        public override ItemStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                var number = reader.GetInt32();
                if (Enum.IsDefined(typeof(ItemStatus), number))
                {
                    return (ItemStatus)number;
                }
                throw new JsonException($"{number} is not a valid ItemStatus value.");
            }

            var value = reader.GetString();
            if (TryParse(value, out var status))
            {
                return status;
            }
            throw new JsonException($"'{value}' is not a valid ItemStatus value.");
        }

        public override void Write(Utf8JsonWriter writer, ItemStatus value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
