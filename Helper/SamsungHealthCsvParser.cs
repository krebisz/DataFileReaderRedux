using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DataFileReader.Helper
{
    /// <summary>
    /// Parser for Samsung Health CSV files
    /// </summary>
    public static class SamsungHealthCsvParser
    {
        /// <summary>
        /// Parses Samsung Health CSV file and extracts health metrics
        /// </summary>
        public static List<HealthMetric> Parse(string filePath, string fileContent)
        {
            var metrics = new List<HealthMetric>();

            try
            {
                var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                if (lines.Count < 2)
                {
                    Console.WriteLine($"CSV file {filePath} has insufficient lines (need at least header row)");
                    return metrics;
                }

                // First line contains metric type identifier
                var metricTypeIdentifier = lines[0].Split(',')[0];
                var metricType = ExtractMetricTypeFromIdentifier(metricTypeIdentifier, filePath);
                var metricIdentifierForFields = ExtractMetricIdentifierForCsvFields(metricTypeIdentifier, filePath);

                // Second line contains column headers
                var headers = ParseCsvLine(lines[1]);

                // Process data rows (starting from line 3, index 2)
                for (int i = 2; i < lines.Count; i++)
                {
                    var values = ParseCsvLine(lines[i]);

                    // Normalize values to match header count
                    // If more values than headers: truncate to header count
                    // If fewer values than headers: pad with empty strings
                    var normalizedValues = NormalizeCsvRow(values, headers.Count);

                    var rowMetrics = ParseCsvRow(headers, normalizedValues, filePath, metricType, metricIdentifierForFields);
                    metrics.AddRange(rowMetrics);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Samsung Health CSV file {filePath}: {ex.Message}");
            }

            return metrics;
        }

        /// <summary>
        /// Normalizes CSV row values to match header count
        /// If more values than headers: truncate to header count
        /// If fewer values than headers: pad with empty strings
        /// </summary>
        private static List<string> NormalizeCsvRow(List<string> values, int headerCount)
        {
            var normalized = new List<string>(values);

            if (normalized.Count > headerCount)
            {
                // More values than headers: truncate to header count
                normalized = normalized.Take(headerCount).ToList();
            }
            else if (normalized.Count < headerCount)
            {
                // Fewer values than headers: pad with empty strings
                while (normalized.Count < headerCount)
                {
                    normalized.Add(string.Empty);
                }
            }

            return normalized;
        }

        /// <summary>
        /// Parses a CSV line handling quoted values
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new System.Text.StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString().Trim());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            // Add the last value
            values.Add(currentValue.ToString().Trim());

            return values;
        }

        /// <summary>
        /// Extracts metric type from identifier or file path
        /// Returns both PascalCase (for display) and original format (for CSV field matching)
        /// </summary>
        private static string ExtractMetricTypeFromIdentifier(string identifier, string filePath)
        {
            if (!string.IsNullOrEmpty(identifier) && identifier.Contains("com.samsung"))
            {
                var metricPart = identifier.Replace("com.samsung.shealth.", "")
                                         .Replace("com.samsung.health.", "")
                                         .Replace(".raw", "")
                                         .Replace(".binning_data", "")
                                         .Replace(".extra_data", "");

                // Return PascalCase for consistency with JSON parser
                return SamsungHealthParser.ToPascalCase(metricPart);
            }

            // Fallback to extracting from file path
            return SamsungHealthParser.ExtractMetricType(filePath);
        }

        /// <summary>
        /// Extracts the original metric identifier format (with dots/underscores) for CSV field matching
        /// Note: CSV fields use "com.samsung.health" while file identifiers may use "com.samsung.shealth"
        /// </summary>
        private static string ExtractMetricIdentifierForCsvFields(string identifier, string filePath)
        {
            string metricPart = null;

            if (!string.IsNullOrEmpty(identifier) && identifier.Contains("com.samsung"))
            {
                // Remove prefixes - handle both shealth and health
                metricPart = identifier.Replace("com.samsung.shealth.", "")
                                      .Replace("com.samsung.health.", "")
                                      .Replace(".raw", "")
                                      .Replace(".binning_data", "")
                                      .Replace(".extra_data", "");
            }
            else
            {
                // Extract from file path
                var parts = filePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (part.StartsWith("com.samsung.", StringComparison.OrdinalIgnoreCase))
                    {
                        metricPart = part.Replace("com.samsung.shealth.", "")
                                         .Replace("com.samsung.health.", "")
                                         .Replace(".raw", "")
                                         .Replace(".binning_data", "")
                                         .Replace(".extra_data", "");
                        break;
                    }
                }
            }

            // CSV fields always use "health" not "shealth", so ensure we have the right format
            // Example: "tracker.heart_rate" from file becomes "heart_rate" in CSV fields
            // But some files have "tracker.heart_rate" and CSV has "heart_rate.start_time"
            // So we need to extract just the metric name part (last segment)
            if (!string.IsNullOrEmpty(metricPart))
            {
                var segments = metricPart.Split('.');
                // If it's like "tracker.heart_rate", use just "heart_rate" for CSV fields
                // But keep the full path for some metrics
                if (segments.Length > 1 && segments[0] == "tracker")
                {
                    return string.Join(".", segments.Skip(1));
                }
                return metricPart;
            }

            return "unknown";
        }

        /// <summary>
        /// Parses a single CSV row and extracts metrics
        /// </summary>
        private static List<HealthMetric> ParseCsvRow(List<string> headers, List<string> values, string filePath, string metricType, string metricIdentifierForFields)
        {
            var metrics = new List<HealthMetric>();

            // Create a dictionary for easy lookup
            var rowData = new Dictionary<string, string>();
            for (int i = 0; i < headers.Count && i < values.Count; i++)
            {
                rowData[headers[i]] = values[i];
            }

            // Extract timestamp fields
            DateTime? timestamp = null;
            string rawTimestamp = "";

            // Try common timestamp field names - check both short and full namespace versions
            // First try metric-specific fields (e.g., com.samsung.health.heart_rate.start_time)
            var timestampFieldPatterns = new List<string>();

            // Add metric-specific patterns using the original identifier format
            if (!string.IsNullOrEmpty(metricIdentifierForFields))
            {
                timestampFieldPatterns.AddRange(new[]
                {
                    $"com.samsung.health.{metricIdentifierForFields}.start_time",
                    $"com.samsung.health.{metricIdentifierForFields}.end_time",
                    $"com.samsung.health.{metricIdentifierForFields}.create_time",
                    $"com.samsung.health.{metricIdentifierForFields}.update_time"
                });
            }

            // Add generic patterns
            timestampFieldPatterns.AddRange(new[]
            {
                "start_time",
                "end_time",
                "create_time",
                "update_time",
                "com.samsung.health.heart_rate.start_time",
                "com.samsung.health.heart_rate.end_time"
            });

            // Try exact matches first
            foreach (var field in timestampFieldPatterns)
            {
                if (rowData.ContainsKey(field) && !string.IsNullOrEmpty(rowData[field]))
                {
                    rawTimestamp = rowData[field];
                    timestamp = TimeNormalizationHelper.ParseTimestamp(rowData[field]);
                    if (timestamp != null)
                    {
                        break;
                    }
                }
            }

            // If no exact match, try partial matches (contains "start_time" or "end_time")
            if (timestamp == null)
            {
                foreach (var kvp in rowData)
                {
                    var fieldName = kvp.Key.ToLower();
                    if ((fieldName.Contains("start_time") || fieldName.Contains("end_time")) &&
                        !string.IsNullOrEmpty(kvp.Value))
                    {
                        rawTimestamp = kvp.Value;
                        timestamp = TimeNormalizationHelper.ParseTimestamp(kvp.Value);
                        if (timestamp != null)
                        {
                            break;
                        }
                    }
                }
            }

            // Extract all numeric values as metrics
            foreach (var kvp in rowData)
            {
                var fieldName = kvp.Key;
                var fieldValue = kvp.Value;

                // Skip timestamp fields (we already have them)
                if (fieldName.Contains("time", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("date", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("uuid", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("source", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("tag_id", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("binning_data", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("comment", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("pkg_name", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("deviceuuid", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("datauuid", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("time_offset", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("create_sh_ver", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("modify_sh_ver", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("custom", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Try to parse as numeric value
                if (decimal.TryParse(fieldValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal numericValue))
                {
                    // Clean up field name (remove namespace prefixes)
                    var cleanFieldName = fieldName;
                    if (cleanFieldName.Contains("com.samsung.health."))
                    {
                        var parts = cleanFieldName.Split('.');
                        cleanFieldName = parts.LastOrDefault() ?? cleanFieldName;
                    }

                    var metric = new HealthMetric
                    {
                        Provider = "Samsung",
                        MetricType = $"{metricType}_{cleanFieldName}",
                        SourceFile = filePath,
                        NormalizedTimestamp = timestamp,
                        RawTimestamp = rawTimestamp,
                        Value = numericValue,
                        Unit = DetermineUnit(cleanFieldName, metricType)
                    };

                    // Store additional fields as metadata
                    foreach (var otherField in rowData)
                    {
                        if (otherField.Key != fieldName &&
                            !otherField.Key.Contains("time", StringComparison.OrdinalIgnoreCase))
                        {
                            metric.AdditionalFields[otherField.Key] = otherField.Value ?? "";
                        }
                    }

                    metrics.Add(metric);
                }
            }

            // If no metrics were extracted but we have a timestamp, create a record
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
                foreach (var field in rowData)
                {
                    metric.AdditionalFields[field.Key] = field.Value ?? "";
                }

                metrics.Add(metric);
            }

            return metrics;
        }

        /// <summary>
        /// Determines unit of measurement based on field name and metric type
        /// </summary>
        private static string DetermineUnit(string fieldName, string metricType)
        {
            return SamsungHealthParser.DetermineUnit(fieldName, metricType);
        }
    }
}

