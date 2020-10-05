# OTT QReview Field Data Plugin

The QReview plugin will work with stock AQTS 2020.2 systems with no special configuration required.

But if you have changed the configuration of some of your AQTS system's configurable "Drop-down Lists", then you may need to tell the QRev plugin how to correctly interpret a few key fields.

## The `Config.json` file stores the plugin's configuration

The plugin can be configured via a [`Config.json`](./Config.json) JSON document, to control the date and time formats used by your organization.

Use the Settings page of the System Config app to change the configuration setting.
- **Group**: `FieldDataPluginConfig-QReview`
- **Key**: `Config`<br/>
- **Value**: The entire contents of the Config.json file. If blank or omitted, the plugin's default [`Config.json`](./Config.json) is used.

This JSON document is reloaded each time a QRev file is uploaded to AQTS for parsing. Updates to the setting will take effect on the next QRev file parsed.

The JSON configuration information stores four settings:

| Property Name | Description |
| --- | --- |
| **Grades** | How to map the QReview `Quality` property to an AQTS grade.<br/><br/>If empty, no measurement grade will be assigned. |
| **DateTimeFormats** | .NET date & time format strings for parsing the `Date/Time` summary field.<br/><br/>If empty, US-English formats will be expected. |
| **TimeFormats** | .NET time format strings for parsing times.<br/><br/>If empty, US-English formats will be expected. |


```json
{
  "Grades": {
  },
  "DateTimeFormats": [],
  "TimeFormats": []
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
