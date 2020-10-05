using System.Collections.Generic;

namespace QReview
{
    public class Config
    {
        public Dictionary<string, string> Grades { get; set; }
        public string[] DateTimeFormats { get; set; }
        public string[] TimeFormats { get; set; }
    }
}
