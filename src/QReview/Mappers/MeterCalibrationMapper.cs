using FieldDataPluginFramework.DataModel.Meters;

namespace QReview.Mappers
{
    public class MeterCalibrationMapper
    {
        public MeterCalibration Map(DischargeMeasurementSummary summary)
        {
            return new MeterCalibration
            {
                Manufacturer = "OTT", // Required
                SerialNumber = summary.SerialNumber, // Required
                Model = summary.Instrument, // Required
                FirmwareVersion = summary.SoftwareVersion,
                MeterType = MeterType.Adcp,
                SoftwareVersion = summary.SoftwareVersion,
                Configuration = ComposeConfiguration(summary)
            };
        }

        private string ComposeConfiguration(DischargeMeasurementSummary summary)
        {
            return $"OTT/{summary.Instrument}/{summary.SerialNumber}";
        }
    }
}
