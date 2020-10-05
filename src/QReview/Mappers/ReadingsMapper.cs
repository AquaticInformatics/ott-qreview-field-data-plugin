using System;
using System.Collections.Generic;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.DataModel.Readings;
using QReview.SystemCode;

namespace QReview.Mappers
{
    public class ReadingsMapper
    {
        private FieldVisitInfo FieldVisitInfo { get; }

        public ReadingsMapper(FieldVisitInfo fieldVisitInfo)
        {
            FieldVisitInfo = fieldVisitInfo ?? throw new ArgumentNullException(nameof(fieldVisitInfo));
        }

        public IEnumerable<Reading> Map(DischargeMeasurementSummary summary)
        {
            if (summary.MeanTemp.HasValue)
            {
                yield return new Reading(Parameters.WaterTemp, Units.Celcius, summary.MeanTemp);
            }
        }
    }
}
