using System;
using System.Collections.Generic;
using System.Linq;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.DataModel;
using FieldDataPluginFramework.DataModel.ChannelMeasurements;
using FieldDataPluginFramework.DataModel.DischargeActivities;
using FieldDataPluginFramework.DataModel.Verticals;
using FieldDataPluginFramework.Units;
using QReview.SystemCode;

namespace QReview.Mappers
{
    internal class DischargeActivityMapper
    {
        private FieldVisitInfo FieldVisitInfo { get; }
        private Config Config { get; }

        public bool IsMetric { get; private set; }

        public DischargeActivityMapper(Config config, FieldVisitInfo fieldVisitInfo)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            FieldVisitInfo = fieldVisitInfo ?? throw new ArgumentNullException(nameof(fieldVisitInfo));
        }

        public DischargeActivity Map(DischargeMeasurementSummary summary)
        {
            IsMetric = summary.IsMetric;

            var unitSystem = IsMetric
                ? Units.MetricUnitSystem
                : Units.ImperialUnitSystem;

            var dischargeActivity = CreateDischargeActivityWithSummary(summary, unitSystem);

            SetDischargeSection(dischargeActivity, summary, unitSystem);

            return dischargeActivity;
        }

        private DischargeActivity CreateDischargeActivityWithSummary(DischargeMeasurementSummary summary,
            UnitSystem unitSystem)
        {
            var factory = new DischargeActivityFactory(unitSystem);

            var totalDischarge = summary.Discharge ??
                                 throw new ArgumentException("No total discharge amount provided");

            //Discharge summary:
            var measurementPeriod = GetMeasurementPeriod();
            var dischargeActivity = factory.CreateDischargeActivity(measurementPeriod, totalDischarge);

            dischargeActivity.Comments = summary.Notes;
            dischargeActivity.Party = summary.Operator;

            if (!Config.IgnoreMeasurementId)
                dischargeActivity.MeasurementId = summary.MeasurementNumber;

            dischargeActivity.QuantitativeUncertainty = summary.UncertaintyPercentage;
            dischargeActivity.ActiveUncertaintyType = dischargeActivity.QuantitativeUncertainty.HasValue
                ? UncertaintyType.Quantitative
                : UncertaintyType.None;

            var qualityIssues = summary
                .Verticals
                .SelectMany(v => v.QualityIssues.Select(qi => $"Vertical {v.Number} at {v.Position} {unitSystem.DistanceUnitId}: {qi}"))
                .ToList();

            dischargeActivity.QualityAssuranceComments = string.Join("\n", qualityIssues);

            if (!string.IsNullOrEmpty(summary.Quality) && Config.Grades.Any())
            {
                if (!Config.Grades.TryGetValue(summary.Quality, out var gradeText))
                {
                    gradeText = summary.Quality;
                }

                dischargeActivity.MeasurementGrade = int.TryParse(gradeText, out var gradeCode)
                    ? Grade.FromCode(gradeCode)
                    : Grade.FromDisplayName(gradeText);
            }

            AddMeanGageHeight(dischargeActivity, summary.GageStart, FieldVisitInfo.StartDate, unitSystem);
            AddMeanGageHeight(dischargeActivity, summary.GageEnd, FieldVisitInfo.EndDate, unitSystem);

            return dischargeActivity;
        }

        private DateTimeInterval GetMeasurementPeriod()
        {
            return new DateTimeInterval(FieldVisitInfo.StartDate, FieldVisitInfo.EndDate);
        }

        private void AddMeanGageHeight(DischargeActivity dischargeActivity, double? stage, DateTimeOffset time, UnitSystem unitSystem)
        {
            if (!stage.HasValue)
                return;

            var measurement = new Measurement(stage.Value, unitSystem.DistanceUnitId);
            var gageHeightMeasurement = new GageHeightMeasurement(measurement, time);

            dischargeActivity.GageHeightMeasurements.Add(gageHeightMeasurement);
        }

        private void SetDischargeSection(DischargeActivity dischargeActivity, DischargeMeasurementSummary summary, UnitSystem unitSystem)
        {
            var dischargeSection = CreateDischargeSectionWithDescription(dischargeActivity, summary, unitSystem);

            dischargeActivity.ChannelMeasurements.Add(dischargeSection);

            dischargeSection.MeterCalibration = new MeterCalibrationMapper()
                .Map(summary);

            SetMappedVerticals(dischargeSection, summary);

            SetChannelObservations(dischargeSection, summary, unitSystem);
        }

        private ManualGaugingDischargeSection CreateDischargeSectionWithDescription(DischargeActivity dischargeActivity,
            DischargeMeasurementSummary summary, UnitSystem unitSystem)
        {
            var factory = new ManualGaugingDischargeSectionFactory(unitSystem);
            var dischargeSection = factory.CreateManualGaugingDischargeSection(
                dischargeActivity.MeasurementPeriod, summary.Discharge ?? throw new ArgumentNullException(nameof(summary.Discharge)));

            //Party: 
            dischargeSection.Party = dischargeActivity.Party;
            dischargeSection.Comments = dischargeActivity.Comments;

            //Discharge method default to mid-section:
            var dischargeMethod = "MID".Equals(summary.DischargeMeasurementMethod, StringComparison.InvariantCultureIgnoreCase)
                ? DischargeMethodType.MidSection
                : DischargeMethodType.MeanSection;

            dischargeSection.DischargeMethod = dischargeMethod;
            dischargeSection.StartPoint = "RIGHT".Equals(summary.StartEdge, StringComparison.InvariantCultureIgnoreCase)
                ? StartPointType.RightEdgeOfWater
                : StartPointType.LeftEdgeOfWater;

            dischargeSection.DeploymentMethod = DeploymentMethodType.Unspecified;

            return dischargeSection;
        }

        private void SetChannelObservations(ManualGaugingDischargeSection dischargeSection, DischargeMeasurementSummary summary,
            UnitSystem unitSystem)
        {
            //River area:
            dischargeSection.AreaUnitId = unitSystem.AreaUnitId;
            dischargeSection.AreaValue = summary.Area;

            //Width:
            dischargeSection.WidthValue = summary.Width;

            //Velocity:
            dischargeSection.VelocityUnitId = unitSystem.VelocityUnitId;
            dischargeSection.VelocityAverageValue = summary.MeanVelocity;
        }

        private void SetMappedVerticals(ManualGaugingDischargeSection dischargeSection, DischargeMeasurementSummary summary)
        {
            if (summary.Verticals.Any())
                dischargeSection.NumberOfVerticals = null;

            var verticals = new VerticalMapper(GetMeasurementPeriod(), dischargeSection.MeterCalibration)
                .MapAll(summary);

            var verticalTypes = new Dictionary<PointVelocityObservationType, int>();

            foreach (var vertical in verticals)
            {
                dischargeSection.Verticals.Add(vertical);

                if (vertical.VelocityObservation.VelocityObservationMethod.HasValue)
                {
                    var verticalType = vertical.VelocityObservation.VelocityObservationMethod.Value;

                    if (!verticalTypes.TryGetValue(verticalType, out var count))
                        count = 0;

                    verticalTypes[verticalType] = 1 + count;
                }
            }

            if (verticalTypes.Any())
            {
                dischargeSection.VelocityObservationMethod = verticalTypes
                    .OrderByDescending(kvp => kvp.Value)
                    .First()
                    .Key;
            }
        }
    }
}
