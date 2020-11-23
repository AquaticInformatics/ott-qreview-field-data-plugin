using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace QReview
{
    public class DischargeMeasurementSummary
    {
        public string StationName { get; set; }
        public string StationNumber { get; set; }
        public string MeasurementNumber { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Operator { get; set; }
        public string Instrument { get; set; }
        public string SerialNumber { get; set; }
        public string SoftwareVersion { get; set; }
        public string Units { get; set; }
        public string MeasurementMethod { get; set; }
        public string DischargeMeasurementMethod { get; set; }
        public double? AveragingTime { get; set; }
        public string StartEdge { get; set; }
        public double? MeanDepth { get; set; }
        public double? RatedDischarge { get; set; }
        public int? NumberOfVerticals { get; set; }
        public double? MeanVelocity { get; set; }
        public double? GageStart { get; set; }
        public double? Width { get; set; }
        // ReSharper disable once InconsistentNaming
        public double? MeanSNR { get; set; }
        public double? GageEnd { get; set; }
        public double? Area { get; set; }
        public double? Discharge { get; set; }
        public double? MeanTemp { get; set; }
        public string Quality { get; set; }
        public double? UncertaintyPercentage { get; set; }
        public string Notes { get; set; }
        public List<VerticalSummary> Verticals { get; } = new List<VerticalSummary>();

        public bool IsMetric => "Metric".Equals(Units, StringComparison.InvariantCultureIgnoreCase);
    }

    public class VerticalSummary
    {
        public int Number { get; set; }
        public DateTime Time { get; set; }
        public int Points { get; set; }
        public double? Position { get; set; }
        public double Depth { get; set; }
        public double? MeanVelocity { get; set; }
        public double? Area { get; set; }
        public double? Discharge { get; set; }
        public double? DischargePortion { get; set; }
        public List<string> QualityIssues { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
    }

    public class TsvParser
    {
        private Config Config { get; }
        private long LineNumber { get; set; }
        private string[] Fields { get; set; }

        public TsvParser(Config config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public DischargeMeasurementSummary Load(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.GetEncoding("Windows-1252"))) // TODO: Better figure out the encoding. It isn't UTF-8
            {
                var tsvParser = new TextFieldParser(reader)
                {
                    TextFieldType = FieldType.Delimited,
                    Delimiters = new[] { "\t" },
                    TrimWhiteSpace = true,
                    HasFieldsEnclosedInQuotes = false,
                };

                var sectionType = SectionType.Unknown;

                var sectionTransitions = new Dictionary<string, SectionType>(StringComparer.InvariantCultureIgnoreCase)
                {
                    {"Discharge Measurement Summary", SectionType.Summary},
                    {"Uncertainty According to ISO 748", SectionType.Uncertainty},
                    {"Depth Sensor", SectionType.DepthSensor},
                    {"Quality Threshold Settings", SectionType.QualitySettings},
                    {"Field Quality Check", SectionType.FieldQualityCheck},
                    {"Notes", SectionType.Notes},
                    {"ADC Warnings", SectionType.ADCWarnings},
                    {"Quality Issues", SectionType.QualityIssues},
                    {"Time Series", SectionType.TimeSeries},
                };

                var summaryFields =
                    new Dictionary<string, Action<string>>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        {"Station Nr.", s => Summary.StationNumber = s},
                        {"Measurement Nr", s => Summary.MeasurementNumber = s},
                        {
                            "Date/Time", s =>
                            {
                                var (startTime, endTime) = ParseStartEndTime(s);

                                Summary.StartTime = startTime;
                                Summary.EndTime = endTime;
                            }
                        },
                        {"Operator:", s => Summary.Operator = s},
                        {"Instrument", s => Summary.Instrument = s},
                        {"Serial Nr.", s => Summary.SerialNumber = s},
                        {"Software version:", s => Summary.SoftwareVersion = s},
                        {"Units", s => Summary.Units = s},
                        {"Measurement method:", s => Summary.MeasurementMethod = s},
                        {"Discharge measurement method:", s => Summary.DischargeMeasurementMethod = s},
                        {"Averaging time:", s => Summary.AveragingTime = ParseAveragingTime(s)},
                        {"Start edge", s => Summary.StartEdge = s},
                        {"Mean depth(m)", s => Summary.MeanDepth = ParseNullableDouble(s)},
                        {"Mean depth(ft)", s => Summary.MeanDepth = ParseNullableDouble(s)},
                        {"Rated Q(m³/s)", s => Summary.RatedDischarge = ParseNullableDouble(s)},
                        {"Rated Q(ft³/s)", s => Summary.RatedDischarge = ParseNullableDouble(s)},
                        {"Nr. of verticals", s => Summary.NumberOfVerticals = ParseNullableInteger(s)},
                        {"Mean Velocity(m/s)", s => Summary.MeanVelocity = ParseNullableDouble(s)},
                        {"Mean Velocity(ft/s)", s => Summary.MeanVelocity = ParseNullableDouble(s)},
                        {"Gage Start:", s => Summary.GageStart = ParseNullableDouble(s)},
                        {"Width(m)", s => Summary.Width = ParseNullableDouble(s)},
                        {"Width(ft)", s => Summary.Width = ParseNullableDouble(s)},
                        {"Mean SNR (dB)", s => Summary.MeanSNR = ParseNullableDouble(s)},
                        {"Gage End:", s => Summary.GageEnd = ParseNullableDouble(s)},
                        {"Area(m²)", s => Summary.Area = ParseNullableDouble(s)},
                        {"Area(ft²)", s => Summary.Area = ParseNullableDouble(s)},
                        {"Discharge(m³/s)", s => Summary.Discharge = ParseNullableDischarge(s)},
                        {"Discharge(ft³/s)", s => Summary.Discharge = ParseNullableDischarge(s)},
                        {
                            "Mean Temp. (°C)", s => Summary.MeanTemp = ParseNullableDouble(s)
                        }, // TODO: Is mean temp really always degC, even when not metric?
                        {"Quality", s => Summary.Quality = s},
                    };

                var uncertaintyFields =
                    new Dictionary<string, Action<string>>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        {"Overall", s => Summary.UncertaintyPercentage = ParsePercentage(s)},
                    };

                var sectionParsers =
                    new Dictionary<SectionType, (Dictionary<string, Action<string>> FieldParsers, Action ParseAction)>
                    {
                        {SectionType.Summary, (summaryFields, ParseSummary)},
                        {SectionType.Uncertainty, (uncertaintyFields, ParseUncertainty)},
                        {SectionType.DepthSensor, (null, ParseDepthSensor)},
                        {SectionType.QualitySettings, (null, ParseQualitySettings)},
                        {SectionType.FieldQualityCheck, (null, ParseFieldQualityCheck)},
                        {SectionType.Notes, (null, ParseNotes)},
                        {SectionType.ADCWarnings, (null, ParseADCWarnings)},
                        {SectionType.QualityIssues, (null, ParseQualityIssues)},
                        {SectionType.TimeSeries, (null, ParseTimeSeries)},
                    };

                var sectionsParsed = new Dictionary<SectionType, int>();
                var fieldsParsed = 0;

                while (!tsvParser.EndOfData)
                {
                    LineNumber = tsvParser.LineNumber;

                    Fields = ReadFields(tsvParser);

                    if (Fields == null)
                        continue;

                    if (Fields.Length == 1)
                    {
                        if (sectionTransitions.TryGetValue(Fields[0], out var newSectionType))
                        {
                            sectionType = newSectionType;
                            continue;
                        }
                    }

                    if (!sectionParsers.TryGetValue(sectionType, out var parser))
                        throw new InvalidOperationException(
                            $"Don't know how to parse line {LineNumber} ({sectionType}): {string.Join(",", Fields)}");

                    if (!sectionsParsed.TryGetValue(sectionType, out var count))
                    {
                        count = 0;
                    }

                    sectionsParsed[sectionType] = 1 + count;

                    if (parser.FieldParsers != null)
                    {
                        for (var i = 0; i < Fields.Length - 1; ++i)
                        {
                            if (string.IsNullOrEmpty(Fields[0]))
                                continue;

                            if (parser.FieldParsers.TryGetValue(Fields[i], out var fieldAction))
                            {
                                fieldAction(Fields[i + 1]);
                                ++i;
                                ++fieldsParsed;
                            }
                        }
                    }

                    parser.ParseAction?.Invoke();
                }

                SortVerticalsByPosition();

                if (fieldsParsed < 5 || sectionsParsed.Count < 2)
                    return null;

                return Summary;
            }
        }

        private static string[] ReadFields(TextFieldParser tsvParser)
        {
            var rawFields = tsvParser.ReadFields();

            if (rawFields == null)
                return null;

            var fields =  new List<string>(rawFields);

            for (var i = fields.Count - 1; i > 0; --i)
            {
                var field = fields[i];

                if (!string.IsNullOrEmpty(field))
                    break;

                fields.RemoveAt(i);
            }

            return fields.ToArray();
        }

        private void SortVerticalsByPosition()
        {
            var verticals = Summary
                .Verticals
                .Where(v => v.Points > 0 || v.Position.HasValue)
                .OrderBy(v => v.Position)
                .ToList();

            Summary.Verticals.Clear();
            Summary.Verticals.AddRange(verticals);
        }

        private enum SectionType
        {
            Unknown,
            Summary,
            Uncertainty,
            DepthSensor,
            QualitySettings,
            FieldQualityCheck,
            Notes,
            // ReSharper disable once InconsistentNaming
            ADCWarnings,
            QualityIssues,
            TimeSeries,
        }

        private DischargeMeasurementSummary Summary { get; } = new DischargeMeasurementSummary();

        private double? ParsePercentage(string text)
        {
            var match = PercentageRegex.Match(text);

            if (!match.Success)
                return null;

            if (double.TryParse(match.Groups["number"].Value, out var value))
                return value;

            throw new ArgumentException($"Line {LineNumber}: '{text}' is not a valid percentage");
        }

        private static readonly Regex PercentageRegex = new Regex(@"\s*(?<number>[\+\-\.0-9]+)\s*%");

        private double? ParseNullableDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (double.TryParse(text, out var value))
                return value;

            throw new ArgumentException($"Line {LineNumber}: '{text}' is not a valid number");
        }

        private double ParseDouble(string text)
        {
            var value = ParseNullableDouble(text);

            return value ?? throw new ArgumentException($"Line {LineNumber}: '{text}' is not a valid number");
        }

        private double? ParseNullableDischarge(string text)
        {
            var match = DischargeRegex.Match(text);

            if (!match.Success)
                return null;

            return ParseNullableDouble(match.Groups["discharge"].Value);
        }

        private static readonly Regex DischargeRegex = new Regex(@"^\s*(?<discharge>\S+)\s+\+/-\s*\S+\s*$");

        private double? ParseAveragingTime(string text)
        {
            var match = AveragingTimeRegex.Match(text);

            if (!match.Success)
                return null;

            return ParseNullableDouble(match.Groups["seconds"].Value);
        }

        private static readonly Regex AveragingTimeRegex = new Regex(@"^\s*(?<seconds>\S+)\s+Seconds$");

        private int? ParseNullableInteger(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (int.TryParse(text, out var value))
                return value;

            throw new ArgumentException($"Line {LineNumber}: '{text}' is not a valid integer");
        }

        private int ParseInteger(string text)
        {
            var value = ParseNullableInteger(text);

            return value ?? throw new ArgumentException($"Line {LineNumber}: '{text}' is not a valid integer");
        }

        private (DateTime? startTime, DateTime? endTime) ParseStartEndTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (null, null);

            var parts = text.Split('>')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            if (parts.Length != 2)
                throw new ArgumentException($"Line {LineNumber}: '{text}' is not a valid start & end time");

            var startText = parts[0];
            var endText = parts[1];

            if (!TryParseDateTime(startText, out var startTime))
                throw new ArgumentException($"Line {LineNumber}: '{startText}' is not a valid datetime.");

            return (startTime, ParseTime(startTime, endText));
        }

        private bool TryParseDateTime(string text, out DateTime time)
        {
            if (!Config.DateTimeFormats.Any())
                return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

            return DateTime.TryParseExact(text, Config.DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
        }

        private DateTime ParseTime(DateTime startTime, string text)
        {
            if (!TryParseTime(text, out var endTime))
                throw new ArgumentException($"Line {LineNumber}: '{text}' is not a valid time.");

            var addDays = endTime.TimeOfDay < startTime.TimeOfDay
                ? 1
                : 0;

            return new DateTime(startTime.Year, startTime.Month, startTime.Day)
                    .AddDays(addDays)
                    .Add(endTime.TimeOfDay);
        }

        private bool TryParseTime(string text, out DateTime time)
        {
            if (!Config.TimeFormats.Any())
                return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

            return DateTime.TryParseExact(text, Config.TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
        }

        private void ParseSummary()
        {
            if (Fields.Length == 1 && string.IsNullOrEmpty(Summary.StationName))
            {
                Summary.StationName = Fields[0];
            }
        }

        private void ParseUncertainty()
        {
        }

        private void ParseDepthSensor()
        {
        }

        private void ParseQualitySettings()
        {
        }

        private void ParseFieldQualityCheck()
        {
        }

        private void ParseNotes()
        {
            var note = string.Join(" ", Fields).Trim();

            if (string.IsNullOrEmpty(note) || SkipNoteMarkers.Contains(note))
                return;

            if (string.IsNullOrEmpty(Summary.Notes))
                Summary.Notes = note;
            else
                Summary.Notes += "\n" + note;
        }

        private static readonly HashSet<string> SkipNoteMarkers =
            new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                "_ General _",
                "_ Verticals _",
            };

        private int CurrentVertical { get; set; }

        private void ParseADCWarnings()
        {
            var match = VerticalRegex.Match(Fields[0]);

            if (match.Success)
            {
                CurrentVertical = ParseInteger(match.Groups["vertical"].Value);
                return;
            }

            var vertical = GetOrAddVertical(CurrentVertical);

            var warning = Fields[0].Trim();

            if (!vertical.Warnings.Contains(warning))
                vertical.Warnings.Add(warning);
        }

        private static readonly Regex VerticalRegex =
            new Regex(@"^\s*Vertical (?<vertical>\d+) at [0-9\.\-\+]+\s*:\s*$");

        private VerticalSummary GetOrAddVertical(int number)
        {
            var vertical = Summary
                .Verticals
                .FirstOrDefault(v => v.Number == number);

            if (vertical != null)
                return vertical;

            vertical = new VerticalSummary
            {
                Number = number
            };

            Summary.Verticals.Add(vertical);

            return vertical;
        }

        private void ParseQualityIssues()
        {
            var match = QualityIssueRegex.Match(Fields[0]);

            if (!match.Success)
                return;

            var vertical = GetOrAddVertical(ParseInteger(match.Groups["vertical"].Value));
            var issue = match.Groups["issue"].Value.Trim();

            if (!vertical.QualityIssues.Contains(issue))
                vertical.QualityIssues.Add(issue);
        }

        private static readonly Regex QualityIssueRegex =
            new Regex(@"^\s*Vertical (?<vertical>\d+) at [0-9\.\-\+]+\s*:\s*(?<issue>.*)$");

        private void ParseTimeSeries()
        {
            if (Fields[0].StartsWith("Time") || string.IsNullOrEmpty(Fields[0]))
                return;

            if (Fields.Length < 9)
                return;

            var time = ParseTime(Summary.StartTime ?? throw new ArgumentException($"Line {LineNumber}: No start time context available"), Fields[0]);

            var vertical = GetOrAddVertical(ParseInteger(Fields[1]));
            vertical.Time = time;
            vertical.Points = ParseInteger(Fields[2]);
            vertical.Position = ParseDouble(Fields[3]);
            vertical.Depth = ParseDouble(Fields[4]);
            vertical.MeanVelocity = ParseNullableDouble(Fields[5]);
            vertical.Area = ParseNullableDouble(Fields[6]);
            vertical.Discharge = ParseNullableDouble(Fields[7]);
            vertical.DischargePortion = ParseNullableDouble(Fields[8].Replace("*", ""));
        }
    }
}