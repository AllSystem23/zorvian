using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zorvian.Web.Json;

/// <summary>
/// Global JsonConverter that ensures all DateTime values are serialized/deserialized
/// with <see cref="DateTimeKind.Utc"/>. This prevents Npgsql from throwing
/// <c>Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone'</c>.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Convert to UTC before serializing to maintain consistent output
        if (value.Kind == DateTimeKind.Local)
            value = value.ToUniversalTime();
        else if (value.Kind == DateTimeKind.Unspecified)
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);

        writer.WriteStringValue(value);
    }
}
