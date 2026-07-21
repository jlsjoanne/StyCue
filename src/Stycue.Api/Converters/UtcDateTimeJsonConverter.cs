using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stycue.Api.Converters
{
    /// <summary>
    /// 將 API 回應中的事件時間統一序列化為 UTC ISO 8601 格式。
    /// SQL Server 讀回的 Unspecified DateTime 依專案規則視為 UTC，不進行時差換算。
    /// </summary>
    public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(
              ref Utf8JsonReader reader,
              Type typeToConvert,
              JsonSerializerOptions options)
        {
            // 保持既有 request body 的 DateTime 解析行為。
            return reader.GetDateTime();
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateTime value,
            JsonSerializerOptions options)
        {
            var utcValue = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),

                // EF Core 從 SQL Server 取回 datetime/datetime2 時通常為 Unspecified。
                // 專案既有資料皆以 UTC 儲存，因此僅標記為 UTC，不可加減時差。
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),

                _ => value
            };

            writer.WriteStringValue(utcValue);
        }
    }
}
