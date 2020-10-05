using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FieldDataPluginFramework.Results;
using ServiceStack;

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
            if (!Settings.TryGetValue(nameof(Config), out var jsonText) || string.IsNullOrWhiteSpace(jsonText))
                return Sanitize(new Config());

            try
            {
                return Sanitize(jsonText.FromJson<Config>());
            }
            catch (SerializationException exception)
            {
                throw new ArgumentException($"Invalid Config JSON:\b{jsonText}", exception);
            }
        }

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
