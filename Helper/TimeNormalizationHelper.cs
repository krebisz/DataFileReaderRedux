using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace DataFileReader.Helper
{
    /// <summary>
    /// Helper class for normalizing timestamps from various formats to standardized DateTime
    /// </summary>
    public static class TimeNormalizationHelper
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts Unix timestamp (milliseconds) to DateTime UTC
        /// </summary>
        public static DateTime? ConvertUnixMilliseconds(long? milliseconds)
        {
            if (milliseconds == null) return null;
            return UnixEpoch.AddMilliseconds(milliseconds.Value);
        }

        /// <summary>
        /// Converts Unix timestamp (seconds) to DateTime UTC
        /// </summary>
        public static DateTime? ConvertUnixSeconds(long? seconds)
        {
            if (seconds == null) return null;
            return UnixEpoch.AddSeconds(seconds.Value);
        }

        /// <summary>
        /// Attempts to parse various timestamp formats and convert to DateTime UTC
        /// </summary>
        public static DateTime? ParseTimestamp(object timestampValue)
        {
            if (timestampValue == null) return null;

            // Try as long (Unix milliseconds)
            if (long.TryParse(timestampValue.ToString(), out long longValue))
            {
                // If it's a reasonable Unix timestamp in milliseconds (after year 2000)
                if (longValue > 946684800000) // Jan 1, 2000 in milliseconds
                {
                    return ConvertUnixMilliseconds(longValue);
                }
                // If it's a reasonable Unix timestamp in seconds (after year 2000)
                else if (longValue > 946684800) // Jan 1, 2000 in seconds
                {
                    return ConvertUnixSeconds(longValue);
                }
            }

            // Try as DateTime string (ISO 8601, etc.)
            if (DateTime.TryParse(timestampValue.ToString(), out DateTime dateTime))
            {
                return dateTime.ToUniversalTime();
            }

            return null;
        }

        /// <summary>
        /// Extracts timestamp from a JToken, checking common field names
        /// </summary>
        public static DateTime? ExtractTimestamp(Newtonsoft.Json.Linq.JToken token, string preferredField = "start_time")
        {
            if (token == null) return null;

            // Try preferred field first
            var preferred = token[preferredField];
            if (preferred != null)
            {
                var dt = ParseTimestamp(preferred.Value<object>());
                if (dt != null) return dt;
            }

            // Try common timestamp field names
            string[] timestampFields = { "timestamp", "date", "time", "datetime", "start_time", "end_time", "created_time", "update_time" };

            foreach (var field in timestampFields)
            {
                var fieldToken = token[field];
                if (fieldToken != null)
                {
                    var dt = ParseTimestamp(fieldToken.Value<object>());
                    if (dt != null) return dt;
                }
            }

            return null;
        }
    }
}

