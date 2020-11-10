using System;
using System.Collections.Generic;
using System.Linq;
using FieldDataPluginFramework.DataModel;
using FieldDataPluginFramework.DataModel.ChannelMeasurements;
using FieldDataPluginFramework.DataModel.Meters;
using FieldDataPluginFramework.DataModel.Verticals;

namespace QReview.Mappers
{
    public class VerticalMapper
    {
        private DateTimeInterval MeasurementInterval { get; }
        private MeterCalibration MeterCalibration { get; }
        private double? ObservationSeconds { get; set; }

        public VerticalMapper(DateTimeInterval measurementInterval, MeterCalibration meterCalibration)
        {
            MeasurementInterval = measurementInterval;
            MeterCalibration = meterCalibration;
        }

        public IEnumerable<Vertical> MapAll(DischargeMeasurementSummary summary)
        {
            if (!summary.Verticals.Any())
                yield break;

            ObservationSeconds = summary.AveragingTime ?? 20;

            foreach (var vertical in summary.Verticals.Select(Map))
            {
                yield return vertical;
            }
        }

        private Vertical Map(VerticalSummary vertical)
        {
            return new Vertical
            {
                Segment = GetSegment(vertical),
                MeasurementConditionData = new OpenWaterData(), // TODO: Support ice?
                TaglinePosition = vertical.Position,
                SequenceNumber = vertical.Number,
                MeasurementTime = GetMeasurementTime(vertical),
                VerticalType = VerticalType.MidRiver, //TODO: is this correct?
                EffectiveDepth = vertical.Depth,
                VelocityObservation = GetVelocityObservation(vertical),
                FlowDirection = FlowDirectionType.Normal,
                Comments = string.Join("\n", vertical.Warnings)
            };
        }

        private Segment GetSegment(VerticalSummary vertical)
        {
            return new Segment
            {
                Area = vertical.Area ?? 0,
                Discharge = vertical.Discharge ?? 0,
                Velocity = vertical.MeanVelocity ?? 0,
                Width = vertical.Area / vertical.Depth ?? 0,
                TotalDischargePortion = vertical.DischargePortion ?? 0,
            };
        }

        private DateTimeOffset? GetMeasurementTime(VerticalSummary vertical)
        {
            return new DateTimeOffset(vertical.Time, MeasurementInterval.Start.Offset);
        }

        private VelocityObservation GetVelocityObservation(VerticalSummary vertical)
        {
            if (!ObservationTypes.TryGetValue(vertical.Points, out var observationType))
                throw new ArgumentException($"A point count of {vertical.Points} is not supported");

            var velocityObservation = new VelocityObservation
            {
                MeterCalibration = MeterCalibration,
                VelocityObservationMethod = observationType.Type,
                MeanVelocity = vertical.MeanVelocity ?? 0,
                DeploymentMethod = DeploymentMethodType.Unspecified,
            };

            foreach (var percentageDepth in observationType.PercentageDepths)
            {
                velocityObservation.Observations.Add(GetVelocityDepthObservation(vertical, percentageDepth));
            }

            return velocityObservation;
        }

        private static readonly Dictionary<int, (PointVelocityObservationType Type, int[] PercentageDepths)> ObservationTypes =
            new Dictionary<int, (PointVelocityObservationType Type, int[] PercentageDepths)>
            {
                {1, (PointVelocityObservationType.OneAtPointSix, new []{60})},
                {2, (PointVelocityObservationType.OneAtPointTwoAndPointEight, new []{20,80})},
                {3, (PointVelocityObservationType.OneAtPointTwoPointSixAndPointEight, new []{20,60,80})}
            };

        private VelocityDepthObservation GetVelocityDepthObservation(VerticalSummary vertical, int percentageDepth)
        {
            return new VelocityDepthObservation
            {
                Depth = vertical.Depth * percentageDepth / 100.0,
                ObservationInterval = ObservationSeconds,
                RevolutionCount = 0,
                Velocity = vertical.MeanVelocity ?? 0
            };
        }
    }
}
