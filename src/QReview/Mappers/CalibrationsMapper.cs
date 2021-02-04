using System;
using System.Collections.Generic;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.DataModel.Calibrations;
using QReview.SystemCode;

namespace QReview.Mappers
{
    public class CalibrationsMapper
    {
        private FieldVisitInfo FieldVisitInfo { get; }

        public CalibrationsMapper(FieldVisitInfo fieldVisitInfo)
        {
            FieldVisitInfo = fieldVisitInfo ?? throw new ArgumentNullException(nameof(fieldVisitInfo));
        }

        public IEnumerable<Calibration> Map(DischargeMeasurementSummary summary)
        {
            if (summary.MeanTemp.HasValue)
            {
                yield return new Calibration(Parameters.WaterTemp, Units.Celcius, summary.MeanTemp.Value)
                {
                    DateTimeOffset = FieldVisitInfo.StartDate
                };
            }
        }
    }
}
