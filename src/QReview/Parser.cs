using FieldDataPluginFramework;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.Results;
using QReview.Mappers;

namespace QReview
{
    public class Parser
    {
        private Config Config { get; }
        private LocationInfo Location { get; }
        private IFieldDataResultsAppender Appender { get; }
        private ILog Logger { get; }

        public Parser(Config config, LocationInfo location, IFieldDataResultsAppender appender, ILog logger)
        {
            Config = config;
            Location = location;
            Appender = appender;
            Logger = logger;
        }

        public void Parse(DischargeMeasurementSummary channel)
        {
            var fieldVisitInfo = AppendMappedFieldVisitInfo(channel, Location);

            AppendMappedMeasurements(channel, fieldVisitInfo);
        }

        private FieldVisitInfo AppendMappedFieldVisitInfo(DischargeMeasurementSummary channel, LocationInfo locationInfo)
        {
            var mapper = new FieldVisitMapper(channel, Location);
            var fieldVisitDetails = mapper.MapFieldVisitDetails();

            Logger.Info($"Successfully parsed one visit '{fieldVisitDetails.FieldVisitPeriod}' for location '{locationInfo.LocationIdentifier}'");

            return Appender.AddFieldVisit(locationInfo, fieldVisitDetails);
        }

        private void AppendMappedMeasurements(DischargeMeasurementSummary summary, FieldVisitInfo fieldVisitInfo)
        {
            var dischargeActivityMapper = new DischargeActivityMapper(Config, fieldVisitInfo);

            Appender.AddDischargeActivity(fieldVisitInfo, dischargeActivityMapper.Map(summary));

            var readingsMapper = new ReadingsMapper(fieldVisitInfo);

            foreach (var reading in readingsMapper.Map(summary))
            {
                Appender.AddReading(fieldVisitInfo, reading);
            }
        }
    }
}
