# OTT QReview Field Data Plugin

The QReview plugin will work with stock AQTS 2020.2 systems with no special configuration required.

But you may need to configure the QReview plugin if:
- Your agency uses custom grade definitions in QReview software or in AQTS. Configure the **Grades** property to match.
- Your agency uses a non-US-English date or time formats when using QReview to export data. Configure the **DateTimeFormats** and **TimeFormats** properties to match.
- Your agency does not use the entire **StationName** field to store the AQTS location identifier. Configure the **LocationIdentifierSeparator** and **LocationIdentifierZeroPaddedDigits** properties to match.

## How the plugin extracts the AQUARIUS location identifier from the `Summary.TSV` file

In order to get a smooth drag-and-drop import experience from AQUARIUS Springboard, the plugin needs to know the AQUARIUS location for which the measurement was taken. The location identifier is derived from the  **Station Name** entered into the OTT ADC or QLiner device.

Out of the box, the plugin uses some configurable rules to extract the AQUARIUS location identifier from the OTT `Station Name` field.

- Strip all trailing text starting with the first underscore `_` encountered. The **LocationIdentifierSeparator** property can be configured to control this behaviour.
- If the remaining text is a number, pad it to the left with up to 6 zeroes. The **LocationIdentifierZeroPaddedDigits** property can be configured to control this behaviour.

These rules will allow for an agency naming standard of "{LocationIdentifier}_{YYYYMMDD}" as a station name pattern. Your agency may use a different naming convention and can configure the rules accordingly.

| Station Name | AQUARIUS Location Identifier | Description |
| --- | --- | --- |
| `000744_191223` | `000744` | A measurement taken on Dec 23rd, 2019. |
| `0744__191223` | `000744` | A measurement taken on Dec 23rd, 2019, but the operator was sloppy and didn't quite follow the standard, not fully using the 6-digit location identifier syntax, and accidentally use two underscores. |
| `084A_200415 (2)` | `084A` | The text before the first underscore wasn't a valid number, so it was not zero padded. It was just used as-is. | 

If the extracted location identifier doesn't exist in AQUARIUS, the measurement file will not be able to be dropped on the main Springboard drop target. But it can still be uploaded to an existing location by launch the Location Manager page and using the Upload tab.

## Configure the plugin using the System Config app

As of v20.2.8, the plugin is configured via individual settings, using the Settings page of the System Config app.

> Earlier versions of plugin were configured via a [`Config.json`](https://github.com/AquaticInformatics/ott-qreview-field-data-plugin/blob/v20.2.7/src/QReview/Readme.md#the-configjson-file-stores-the-plugins-configuration) JSON document stored as a single setting. Customers were finding the JSON configuration syntax a bit too complex, so v20.2.8 switched to a simpler configuration format, using a separate setting `Key` for each configurable property.

Any changes made to the setting will take effect when the next QReview file parsed.

The following settings are supported for the `FieldDataPluginConfig-QReview` setting group:

| Key | Description | Default value |
| --- | --- | --- |
| **LocationIdentifierSeparator** | A text string that will separate a location identifier from other text in the Station Name.<br/><br/>Use an empty/blank value to use the entire `Station Name` field. | Defaults to an underscore `_` if the setting does not exist. |
| **LocationIdentifierZeroPaddedDigits** | The number of zero-padded digits to use for a numeric location identifier. | Defaults to `6` if the setting does not exist. |
| **IgnoreMeasurementId** | When `false`, the `Measurement Nr` field will be set as the AQTS measurement ID value, which must be unique per location.<br/>When `true`, the `Measurement Nr` field of the `Summary.TSV` file will be ignored. | Defaults to `false` if the setting does not exist. |
| **DateTimeFormats** | A comma-separated list of .NET date & time format strings for parsing the `Date/Time` summary field. | If the setting does not exist or is empty, US-English formats will be expected. (month/day/year) |
| **TimeFormats** | A comma-separated list of .NET time format strings for parsing times. | If the setting does not exist or is empty, US-English formats will be expected. |
| **Grades** | A comma-separated list of QReview `Quality` to AQTS grade mappings, in `{Quality}:{GradeCode}` or `{Quality}:{GradeName}` format.<br/><br/>A value of `UNKNOWN:POOR, GOOD:85` will map the `UNKNOWN` quality to the `POOR` grade and the `GOOD` quality to grade code 85. <br/><br/>When the `Quality` value is not in the `Grades` map, the value is assumed to map 1:1 to a grade code or name. | If the setting does not exist or is empty, no measurement grade will be assigned. |


