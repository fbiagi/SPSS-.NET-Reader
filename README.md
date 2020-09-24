# Spssly

C# SPSS SAV file reader and writer library.

A library that enables reading and writing of SPSS data files (.sav) from and to a Stream. The library is UTF-8 safe.

This project is a fork of [SPSS-.NET-Reader](https://github.com/fbiagi/SPSS-.NET-Reader) by fbiagi (based on [spsslib-80132](https://archive.codeplex.com/?p=spsslib) by elmarj). 

Since forking:
- a lot of bugs have been fixed
- code has been cleaned up
- project ported to .NET Standard 
- .NET Core compatible
- other utlities added to decrease file size

_This library has been battle tested in production on a few large deployments at MMR Research WorldWide._

## Installation

Via Package Manager:
```
Install-Package Spssly
```

Via .NET CLI
```
dotnet add package Spssly
```

### Reading a data file:

```C#
// Open file, can be read only and sequetial (for performance), or anything else
using (FileStream fileStream = new FileStream("data.sav", FileMode.Open, FileAccess.Read, FileShare.Read, 2048*10, 
                                              FileOptions.SequentialScan))
{
    // Create the reader, this will read the file header
    SpssReader spssDataset = new SpssReader(fileStream);
    
    // Iterate through all the varaibles
    foreach (var variable in spssDataset.Variables)
    {
        // Display name and label
        Console.WriteLine("{0} - {1}", variable.Name, variable.Label);
        // Display value-labels collection
        foreach (KeyValuePair<double, string> label in variable.ValueLabels)
        {
            Console.WriteLine(" {0} - {1}", label.Key, label.Value);
        }
    }
    
    // Iterate through all data rows in the file
    foreach (var record in spssDataset.Records)
    {
        foreach (var variable in spssDataset.Variables)
        {
            Console.Write(variable.Name);
            Console.Write(':');
            // Use the corresponding variable object to get the values.
            Console.Write(record.GetValue(variable));
            // This will get the missing values as null, text with out extra spaces,
            // and date values as DateTime.
            // For original values, use record[variable] or record[int]
            Console.Write('\t');
        }
        Console.WriteLine("");
    }
}
```

### Writing a data file:
```C#
// Create Variable list
var variables = new List<Variable>
{
    new Variable
    {
        Label = "The variable Label",
        ValueLabels = new Dictionary<double, string>
                {
                    {1, "Label for 1"},
                    {2, "Label for 2"},
                },
        Name = "avariablename_01",
        PrintFormat = new OutputFormat(FormatType.F, 8, 2),
        WriteFormat = new OutputFormat(FormatType.F, 8, 2),
        Type = DataType.Numeric,
        Width = 10,
        MissingValueType = MissingValueType.NoMissingValues
    },
    new Variable
    {
        Label = "Another variable",
        ValueLabels = new Dictionary<double, string>
                    {
                        {1, "this is 1"},
                        {2, "this is 2"},
                    },
        Name = "avariablename_02",
        PrintFormat = new OutputFormat(FormatType.F, 8, 2),
        WriteFormat = new OutputFormat(FormatType.F, 8, 2),
        Type = DataType.Numeric,
        Width = 10,
        MissingValueType = MissingValueType.OneDiscreteMissingValue
    }
};
// Set the one special missing value
variables[1].MissingValues[0] = 999;  

// Default options
var options = new SpssOptions();

using (FileStream fileStream = new FileStream("data.sav", FileMode.Create, FileAccess.Write))
{
    using (var writer = new SpssWriter(fileStream, variables, options))
    {
        // Create and write records
        var newRecord = writer.CreateRecord();
        newRecord[0] = 15d;
        newRecord[1] = 15.5d;
        writer.WriteRecord(newRecord);
        
        newRecord = writer.CreateRecord();
        newRecord[0] = null;
        newRecord[1] = 200d;
        writer.WriteRecord(newRecord);
        writer.EndFile();
    }
}
```

If you find any bugs or have issues, please open an issue on GitHub. 

## License
Spssly is provided as-is under the MIT license. For more information see [LICENSE](LICENSE).
