using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataFileReader.Helper
{
    /// <summary>
    /// Helper class to parse MetricType and extract base type and subtypes
    /// </summary>
    public static class MetricTypeParser
    {
        /// <summary>
        /// Parses a MetricType string and extracts the base type and subtypes.
        /// Non-alphanumeric characters are used as delimiters to split into parts.
        /// The first part becomes the base type, and subsequent parts become subtypes.
        /// </summary>
        /// <param name="metricType">The full MetricType string (e.g., "TrackerHeartRate_heart_rate" or "Sleep:duration:quality")</param>
        /// <returns>A tuple containing the base type and a list of subtypes</returns>
        public static (string BaseType, List<string> Subtypes) ParseMetricType(string metricType)
        {
            if (string.IsNullOrWhiteSpace(metricType))
            {
                return (string.Empty, new List<string>());
            }

            // Split by any non-alphanumeric character
            var parts = Regex.Split(metricType, @"[^a-zA-Z0-9]+")
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();

            if (parts.Count == 0)
            {
                return (metricType, new List<string>());
            }

            var baseType = parts[0];
            var subtypes = parts.Skip(1).ToList();

            return (baseType, subtypes);
        }

        /// <summary>
        /// Gets the base type from a MetricType string
        /// </summary>
        public static string GetBaseType(string metricType)
        {
            var (baseType, _) = ParseMetricType(metricType);
            return baseType;
        }

        /// <summary>
        /// Gets the subtypes from a MetricType string
        /// </summary>
        public static List<string> GetSubtypes(string metricType)
        {
            var (_, subtypes) = ParseMetricType(metricType);
            return subtypes;
        }

        /// <summary>
        /// Gets a combined subtype string (all subtypes joined with the original delimiter pattern)
        /// Returns null if no subtypes exist
        /// </summary>
        public static string GetSubtypeString(string metricType)
        {
            var (_, subtypes) = ParseMetricType(metricType);
            if (subtypes == null || subtypes.Count == 0)
            {
                return null;
            }

            // Try to preserve the original delimiter pattern
            var delimiter = ExtractDelimiter(metricType);
            return string.Join(delimiter, subtypes);
        }

        /// <summary>
        /// Extracts the delimiter pattern used in the original MetricType
        /// </summary>
        private static string ExtractDelimiter(string metricType)
        {
            var match = Regex.Match(metricType, @"[^a-zA-Z0-9]+");
            return match.Success ? match.Value : "_";
        }
    }
}



