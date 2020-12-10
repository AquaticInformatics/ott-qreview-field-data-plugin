# OTT QReview Field Data Plugin

The QReview plugin will work with stock AQTS 2020.2 systems with no special configuration required.

But you may need to configure the QReview plugin if:
- Your agency uses custom grade definitions in QReview software or in AQTS. Configure the **Grades** property to match.
- Your agency uses a non-US-English date or time formats when using QReview to export data. Configure the **DateTimeFormats** and **TimeFormats** properties to match.
- Your agency does not use the entire **StationName** field to store the AQTS location identifier. Configure the **LocationIdentifierSeparator** and **LocationIdentifierZeroPaddedDigits** properties to match.

## How the plugin extracts the AQUARIUS location identifier from the `Summary.TSV` file

In order to get a smooth drag-and-drop import experience from AQUARIUS Springboard, the plugin needs to know the AQUARIUS location for which the measurement was taken. The location identifier is derived from the  **Station Name** entered into the OTT ADC or QLiner device.

Out of the box, the plugin uses some configurable rules to extract the AQUARIUS location identifier from the OTT Station Name field.

- Strip all trailing text starting with the first underscore `_` encountered. The **LocationIdentifierSeparator** property can be configured to control this behaviour.
- If the remaining text is a number, pad it to the left with up to 6 zeroes. The **LocationIdentifierZeroPaddedDigits** property can be configured to control this behaviour.

These rules will allow for an agency naming standard of "{LocationIdentifier}_{YYYYMMDD}" as a station name pattern. Your agency may use a different naming convention and can configure the rules accordingly.

| Station Name | AQUARIUS Location Identifier | Description |
| --- | --- | --- |
| `000744_191223` | `000744` | A measurement taken on Dec 23rd, 2019. |
| `0744__191223` | `000744` | A measurement taken on Dec 23rd, 2019, but the operator was sloppy and didn't quite follow the standard, not fully using the 6-digit location identifier syntax, and accidentally use two underscores. |
| `084A_200415 (2)` | `084A` | The text before the first underscore wasn't a valid number, so it was not zero padded. It was just used as-is. | 

If the extracted location identifier doesn't exist in AQUARIUS, the measurement file will not be able to be dropped on the main Springboard drop target. But it can still be uploaded to an existing location by launch the Location Manager page and using the Upload tab.

## The `Config.json` file stores the plugin's configuration

The plugin can be configured via a [`Config.json`](./Config.json) JSON document, to control the date and time formats used by your organization.

Use the Settings page of the System Config app to change the configuration setting.
- **Group**: `FieldDataPluginConfig-QReview`
- **Key**: `Config`<br/>
- **Value**: The entire contents of the Config.json file. If blank or omitted, the plugin's default [`Config.json`](./Config.json) is used.

This JSON document is reloaded each time a QReview file is uploaded to AQTS for parsing. Updates to the setting will take effect on the next QReview file parsed.

The JSON configuration information stores five settings:

| Property Name | Description |
| --- | --- |
| **LocationIdentifierSeparator** | A text string that will separate a location identifier from other text in the Station Name.<br/><br/>Use an empty string `""` to use the entire Station Name.<br/><br/>Defaults to an underscore `"_"` if the property is not specified.|
| **LocationIdentifierZeroPaddedDigits** | The number of zero-padded digits to use for a numeric location identifier.<br/><br/>Defaults to `6` if the property is not specified. |
| **Grades** | How to map the QReview `Quality` property to an AQTS grade.<br/><br/>If not empty, the `Quality` value will be mapped to a `GradeCode` if it is an integer number, or to a `GradeName` otherwise. When the `Quality` value is not in the `Grades` map, the value is assumed to map 1:1 to a grade code or name.<br/><br/>If empty, no measurement grade will be assigned. |
| **DateTimeFormats** | .NET date & time format strings for parsing the `Date/Time` summary field.<br/><br/>If empty, US-English formats will be expected. (month/day/year) |
| **TimeFormats** | .NET time format strings for parsing times.<br/><br/>If empty, US-English formats will be expected. |

```json
{
  "LocationIdentifierSeparator": "_",
  "LocationIdentifierZeroPaddedDigits": 6,
  "Grades": {
    "UNKNOWN": "POOR",
    "GOOD": "85"
  },
  "DateTimeFormats": [ "dd-MM-yyyy HH:mm:ss" ],
  "TimeFormats": [ "HH:mm:ss" ]
}
```

Notes:
- Editing JSON files [can be tricky](#json-editing-tips). Don't include a trailing comma after the last item in any list.

## Tips about `Format` strings:
`Format` values are [.NET custom date/time format strings](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings).

These format strings can be rather fussy to deal with, so take care to consider some of the common edge cases:
- Format strings are case-sensitive. Common mistakes are made for month-vs-minute and 24-hour-vs-12-hour patterns.
- Uppercase ['M'](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings#M_Specifier) matches month digits, between 1 and 12.
- Lowercase ['m'](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings#mSpecifier) matches minute digits, between 0 and 59.
- Uppercase ['H'](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings#H_Specifier) matches 24-hour hour digits, between 0 and 23.
- Lowercase ['h'](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings#hSpecifier) matches 12-hour hour digits, between 1 and 12, and require a ['t' or 'tt'](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings#tSpecifier) pattern to distinguish AM from PM.
- Prefer single-character patterns when possible, since they match double-digit values as well. Eg. 'H:m' will match '2:35' and '14:35', but 'HH:mm" will not match '2:35' since the 'HH' means exactly-2-digits.

## JSON editing tips

Editing [JSON](https://json.org) can be a tricky thing.

Sometimes the plugin code can detect a poorly formatted JSON document and report a decent error, but sometimes a poorly formatted JSON document will appear to the plugin as just an empty document.

Here are some tips to help eliminate common JSON config errors:
- Edit JSON in a real text editor. Notepad is fine, [Notepad++](https://notepad-plus-plus.org/) or [Visual Studio Code](https://code.visualstudio.com/) are even better choices.
- Don't try editing JSON in Microsoft Word. Word will mess up your quotes and you'll just have a bad time.
- Try validating your JSON using the online [JSONLint validator](https://jsonlint.com/).
- Whitespace between items is ignored. Your JSON document can be single (but very long!) line, but the convention is separate items on different lines, to make the text file more readable.
- All property names must be enclosed in double-quotes (`"`). Don't use single quotes (`'`) or smart quotes (`“` or `”`), which are actually not that smart for JSON!
- Avoid a trailing comma in lists. JSON is very fussy about using commas **between** list items, but rejects lists when a trailing comma is included. Only use a comma to separate items in the middle of a list.

### Adding comments to JSON

The JSON spec doesn't support comments, which is unfortunate.

However, the code will simply skip over properties it doesn't care about, so a common trick is to add a dummy property name/value string. The code won't care or complain, and you get to keep some notes close to other special values in your custom JSON document.

Instead of this:

```json
{
  "ExpectedPropertyName": "a value",
  "AnotherExpectedProperty": 12.5 
}
```

Try this:

```json
{
  "_comment_": "Don't enter a value below 12, otherwise things break",
  "ExpectedPropertyName": "a value",
  "AnotherExpectedProperty": 12.5 
}
```

Now your JSON has a comment to help you remember why you chose the `12.5` value.
