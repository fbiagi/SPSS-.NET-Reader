# C# SPSS SAV file reader and writer library

This library enables to read and write SPSS data files (.sav) on .net from and to a Stream. The library is UTF-8 safe.

It is available as a nuget package at https://www.nuget.org/packages/SpssLib, and can be installed using the package manager or by issueing:
```
Install-Package SpssLib
``` 

It's a fork of [spsslib-80132](http://spsslib.codeplex.com/) by elmarj. Since forking we have added writing ability, solved many bugs, provided closer SPSS binary integration by following the [PSPP provided SPSS format](https://www.gnu.org/software/pspp/pspp-dev/html_node/System-File-Format.html#System-File-Format). 

This library has been tested in production on a few large deployments at @tns_global.

### To read a data file:

```C#
// Open file, can be read only and sequetial (for performance), or anything else
using (FileStream fileStream = new FileStream("data.sav", FileMode.Open, FileAccess.Read, FileShare.Read, 2048*10, 
                                              FileOptions.SequentialScan))
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

### To write a data file:
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
