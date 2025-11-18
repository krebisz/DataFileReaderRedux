using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DataFileReader.Helper
{
    /// <summary>
    /// Represents a standardized health metric record
    /// </summary>
    public class HealthMetric
    {
        public string Provider { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
        public string MetricSubtype { get; set; } = string.Empty;
        public string SourceFile { get; set; } = string.Empty;
        public DateTime? NormalizedTimestamp { get; set; }
        public string RawTimestamp { get; set; } = string.Empty;
        public decimal? Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalFields { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Parser for Samsung Health JSON files
    /// </summary>
    public static class SamsungHealthParser
    {
        /// <summary>
        /// Detects if a file path belongs to Samsung Health
        /// </summary>
        public static bool IsSamsungHealthFile(string filePath)
        {
            return filePath.Contains("Samsung Health", StringComparison.OrdinalIgnoreCase) ||
                   filePath.Contains("com.samsung", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Extracts metric type from file path
        /// Example: "com.samsung.shealth.tracker.heart_rate" -> "HeartRate"
        /// </summary>
        public static string ExtractMetricType(string filePath)
        {
            var parts = filePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (part.StartsWith("com.samsung.", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the metric name part
                    var metricPart = part.Replace("com.samsung.shealth.", "")
                                         .Replace("com.samsung.health.", "")
                                         .Replace(".raw", "")
                                         .Replace(".binning_data", "")
                                         .Replace(".extra_data", "");

                    // Convert to PascalCase
                    return ToPascalCase(metricPart);
                }
            }

            return "Unknown";
        }

        public static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var parts = input.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower()));
        }

        /// <summary>
        /// Parses Samsung Health JSON file and extracts health metrics
        /// </summary>
        public static List<HealthMetric> Parse(string filePath, string fileContent)
        {
            var metrics = new List<HealthMetric>();

            try
            {
                var jsonToken = JToken.Parse(fileContent);
                var metricType = ExtractMetricType(filePath);

                // Handle array of measurements (most common format)
                if (jsonToken is JArray jsonArray)
                {
                    foreach (var item in jsonArray)
                    {
                        var metric = ParseMeasurementObject(item, filePath, metricType);
                        if (metric != null)
                        {
                            metrics.AddRange(metric);
                        }
                    }
                }
                // Handle single object
                else if (jsonToken is JObject jsonObject)
                {
                    var metric = ParseMeasurementObject(jsonObject, filePath, metricType);
                    if (metric != null)
                    {
                        metrics.AddRange(metric);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Samsung Health file {filePath}: {ex.Message}");
            }

            return metrics;
        }

        /// <summary>
        /// Parses a single measurement object and extracts all metrics from it
        /// </summary>
        private static List<HealthMetric> ParseMeasurementObject(JToken measurement, string filePath, string metricType)
        {
            var metrics = new List<HealthMetric>();

            if (measurement is not JObject obj) return metrics;

            // Extract timestamp
            var timestamp = TimeNormalizationHelper.ExtractTimestamp(obj);
            var rawTimestamp = obj["start_time"]?.ToString() ?? obj["end_time"]?.ToString() ?? "";

            // Extract all numeric values that could be metrics
            foreach (var property in obj.Properties())
            {
                var propName = property.Name;
                var propValue = property.Value;

                // Skip timestamp fields (we already have them)
                if (propName.Contains("time", StringComparison.OrdinalIgnoreCase) ||
                    propName.Contains("date", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Extract numeric values
                if (propValue.Type == JTokenType.Float || propValue.Type == JTokenType.Integer)
                {
                    if (decimal.TryParse(propValue.ToString(), out decimal value))
                    {
                        var metric = new HealthMetric
                        {
                            Provider = "Samsung",
                            MetricType = $"{metricType}_{propName}",
                            SourceFile = filePath,
                            NormalizedTimestamp = timestamp,
                            RawTimestamp = rawTimestamp,
                            Value = value,
                            Unit = DetermineUnit(propName, metricType)
                        };

                        // Store additional fields
                        foreach (var otherProp in obj.Properties())
                        {
                            if (otherProp.Name != propName &&
                                !otherProp.Name.Contains("time", StringComparison.OrdinalIgnoreCase))
                            {
                                metric.AdditionalFields[otherProp.Name] = otherProp.Value?.ToString() ?? "";
                            }
                        }

                        metrics.Add(metric);
                    }
                }
            }

            // If no metrics were extracted but we have a timestamp, create a record with the whole object as metadata
            if (metrics.Count == 0 && timestamp != null)
            {
                var metric = new HealthMetric
                {
                    Provider = "Samsung",
                    MetricType = metricType,
                    SourceFile = filePath,
                    NormalizedTimestamp = timestamp,
                    RawTimestamp = rawTimestamp
                };

                // Store all fields as additional data
                foreach (var prop in obj.Properties())
                {
                    metric.AdditionalFields[prop.Name] = prop.Value?.ToString() ?? "";
                }

                metrics.Add(metric);
            }

            return metrics;
        }

        /// <summary>
        /// Determines unit of measurement based on field name and metric type
        /// </summary>
        public static string DetermineUnit(string fieldName, string metricType)
        {
            var lowerName = fieldName.ToLower();
            var lowerType = metricType.ToLower();

            if (lowerName.Contains("heart_rate") || lowerType.Contains("heartrate"))
                return "bpm";
            if (lowerName.Contains("step") || lowerType.Contains("step"))
                return "steps";
            if (lowerName.Contains("calorie") || lowerType.Contains("calorie"))
                return "kcal";
            if (lowerName.Contains("distance"))
                return "m";
            if (lowerName.Contains("weight") || lowerType.Contains("weight"))
                return "kg";
            if (lowerName.Contains("height"))
                return "cm";
            if (lowerName.Contains("temperature"))
                return "°C";
            if (lowerName.Contains("oxygen") || lowerType.Contains("oxygen"))
                return "%";
            if (lowerName.Contains("pressure"))
                return "mmHg";

            return "";
        }
    }
}

