using System;
using System.Linq;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.DataModel;

namespace QReview.Mappers
{
    public class FieldVisitMapper
    {
        private DischargeMeasurementSummary Summary { get; }
        private TimeSpan UtcOffset { get; }

        public FieldVisitMapper(DischargeMeasurementSummary channel, LocationInfo location)
        {
            Summary = channel ?? throw new ArgumentNullException(nameof(channel));

            UtcOffset = location.UtcOffset;
        }

        public FieldVisitDetails MapFieldVisitDetails()
        {
            var visitPeriod = GetVisitTimePeriod();

            return new FieldVisitDetails(visitPeriod);
        }

        private DateTimeInterval GetVisitTimePeriod()
        {
            var times = new []{Summary.StartTime, Summary.EndTime}
                .Concat(Summary.Verticals.Select(v => (DateTime?)v.Time))
                .Where(t => t != null)
                .Select(t => new DateTimeOffset(t.Value, UtcOffset))
                .OrderBy(dt => dt)
                .ToList();

            if (!times.Any())
                throw new ArgumentException($"Can't parse any timestamps");

            return new DateTimeInterval(times.First(), times.Last());
        }
    }
}
