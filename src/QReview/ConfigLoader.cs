using System;
using System.Collections.Generic;
using System.Linq;
using FieldDataPluginFramework.Results;

namespace QReview
{
    public class ConfigLoader
    {
        private Dictionary<string,string> Settings { get; }

        public ConfigLoader(IFieldDataResultsAppender appender)
        {
            Settings = appender.GetPluginConfigurations();
        }

        public Config Load()
        {
            var config = new Config
            {
                LocationIdentifierSeparator = GetNullableString(nameof(Config.LocationIdentifierSeparator)) ?? Config.DefaultLocationIdentifierSeparator,
                LocationIdentifierZeroPaddedDigits = GetNullableInteger(nameof(Config.LocationIdentifierZeroPaddedDigits)) ?? Config.DefaultLocationIdentifierZeroPaddedDigits,
                IgnoreMeasurementId = GetNullableBoolean(nameof(Config.IgnoreMeasurementId)) ?? Config.DefaultIgnoreMeasurementId,
                DateTimeFormats = GetStrings(nameof(Config.DateTimeFormats)),
                TimeFormats = GetStrings(nameof(Config.TimeFormats)),
                Grades = GetMap(nameof(Config.Grades)),
            };

            return Sanitize(config);
        }

        private string GetNullableString(string key)
        {
            return Settings.TryGetValue(key, out var value) ? value : null;
        }

        private int? GetNullableInteger(string key)
        {
            var text = GetNullableString(key);

            if (int.TryParse(text, out var value))
                return value;

            return null;
        }


        private bool? GetNullableBoolean(string key)
        {
            var text = GetNullableString(key);

            if (bool.TryParse(text, out var value))
                return value;

            return null;
        }

        private string[] GetStrings(string key)
        {
            var text = GetNullableString(key);

            if (string.IsNullOrWhiteSpace(text))
                return null;

            return text
                .Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }

        private Dictionary<string, string> GetMap(string key)
        {
            var strings = GetStrings(key);

            return strings?.Select(s => s.Split(MapSeparators, 2))
                .Where(values => values.Length == 2)
                .ToDictionary(
                    values => values[0],
                    values => values[1]);
        }

        private static readonly char[] MapSeparators = {':'};

        private Config Sanitize(Config config)
        {
            config.Grades = SanitizeMethods(config.Grades, new Dictionary<string, string>());
            config.DateTimeFormats = SanitizeList(config.DateTimeFormats, new List<string>());
            config.TimeFormats = SanitizeList(config.TimeFormats, new List<string>());

            return config;
        }

        private static Dictionary<string,string> SanitizeMethods(Dictionary<string,string>methods, Dictionary<string,string> defaultIfEmpty)
        {
            if (defaultIfEmpty == null)
                throw new ArgumentNullException(nameof(defaultIfEmpty));

            if (methods == null || !methods.Any())
            {
                methods = defaultIfEmpty;
            }

            return methods
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    StringComparer.InvariantCultureIgnoreCase);
        }

        private static string[] SanitizeList(string[] list, List<string> defaultIfEmpty)
        {
            if (defaultIfEmpty == null)
                throw new ArgumentNullException(nameof(defaultIfEmpty));

            if (list == null || !list.Any())
            {
                list = defaultIfEmpty.ToArray();
            }

            return list;
        }
    }
}
