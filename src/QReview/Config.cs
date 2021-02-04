using System.Collections.Generic;

namespace QReview
{
    public class Config
    {
        public const string DefaultLocationIdentifierSeparator = "_";
        public const int DefaultLocationIdentifierZeroPaddedDigits = 6;
        public const bool DefaultIgnoreMeasurementId = false;

        public string LocationIdentifierSeparator { get; set; }
        public int LocationIdentifierZeroPaddedDigits { get; set; }
        public bool IgnoreMeasurementId { get; set; }
        public Dictionary<string, string> Grades { get; set; }
        public string[] DateTimeFormats { get; set; }
        public string[] TimeFormats { get; set; }
    }
}
