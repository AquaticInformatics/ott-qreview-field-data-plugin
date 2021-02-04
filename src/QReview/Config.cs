using System.Collections.Generic;

namespace QReview
{
    public class Config
    {
        public string LocationIdentifierSeparator { get; set; } = "_";
        public int LocationIdentifierZeroPaddedDigits { get; set; } = 6;
        public bool IgnoreMeasurementId { get; set; }
        public Dictionary<string, string> Grades { get; set; }
        public string[] DateTimeFormats { get; set; }
        public string[] TimeFormats { get; set; }
    }
}
