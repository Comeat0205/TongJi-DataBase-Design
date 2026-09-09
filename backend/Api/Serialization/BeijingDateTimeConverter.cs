using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Serialization;

/// <summary>
/// 业务时间一律按北京墙钟序列化（不写 Z），避免前端把 UTC 再转换一次。
/// </summary>
public sealed class BeijingDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss";

    private static readonly TimeZoneInfo Beijing = ResolveBeijingTimeZone();

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return default;
        }

        if (DateTimeOffset.TryParse(text, out var dto))
        {
            return TimeZoneInfo.ConvertTime(dto, Beijing).DateTime;
        }

        if (DateTime.TryParse(text, out var dt))
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        }

        throw new JsonException($"无法解析时间：{text}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var wallClock = value.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(value, Beijing),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(value, Beijing),
            _ => value,
        };

        writer.WriteStringValue(wallClock.ToString(Format));
    }

    private static TimeZoneInfo ResolveBeijingTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "China Standard Time" : "Asia/Shanghai");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                "Asia/Shanghai",
                TimeSpan.FromHours(8),
                "China Standard Time",
                "China Standard Time");
        }
    }
}

public sealed class BeijingNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private readonly BeijingDateTimeConverter _inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return _inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        _inner.Write(writer, value.Value, options);
    }
}
