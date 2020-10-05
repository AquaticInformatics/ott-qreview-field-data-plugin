using System;
using System.IO;
using FieldDataPluginFramework;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.Results;

namespace QReview
{
    public class Plugin : IFieldDataPlugin
    {
        public ParseFileResult ParseFile(Stream fileStream, IFieldDataResultsAppender appender, ILog logger)
        {
            return ParseFile(fileStream, null, appender, logger);
        }

        private Config Config { get; set; }

        public ParseFileResult ParseFile(Stream fileStream, LocationInfo targetLocation, IFieldDataResultsAppender appender, ILog logger)
        {
            try
            {
                Config = new ConfigLoader(appender)
                    .Load();

                var summary = GetSummary(fileStream, logger);

                if (summary == null)
                    return ParseFileResult.CannotParse();

                if (targetLocation == null)
                {
                    if (string.IsNullOrEmpty(summary.StationName))
                        return ParseFileResult.SuccessfullyParsedButDataInvalid("Missing station name");

                    targetLocation = appender.GetLocationByIdentifier(summary.StationName);
                }

                var parser = new Parser(Config, targetLocation, appender, logger);

                parser.Parse(summary);

                return ParseFileResult.SuccessfullyParsedAndDataValid();
            }
            catch (Exception e)
            {
                return ParseFileResult.SuccessfullyParsedButDataInvalid(e.Message);
            }
        }

        private DischargeMeasurementSummary GetSummary(Stream fileStream, ILog logger)
        {
            try
            {
                return new TsvParser(Config)
                    .Load(fileStream);
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                return null;
            }
        }
    }
}
