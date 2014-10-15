using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpssLib.DataReader;
using SpssLib.SpssDataset;

namespace Test.SpssLib
{
	[TestClass]
    public class TestSpssWrtier
    {

		[TestMethod]
        public void TestWriteNumbers()
        {
            var filename = @"C:\Tests\testWriteNumbers.sav";

			using (FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
			{

				var variable1 = new Variable
					{
						Label = "The variable Label",
						ValueLabels = new Dictionary<double, string>
							{
								{1, "Label for 1"},
								{2, "Label for 2"},
							},
						Name = "avariablename_01",
						PrintFormat = new OutputFormat(0x00050802), // TODO improve the output format creation
						WriteFormat = new OutputFormat(0x00050802),
						Type = DataType.Numeric,
						Width = 10,
						MissingValues = new List<double>{999},
					};
				var variable2 = new Variable
					{
						Label = "Another variable",
						ValueLabels = new Dictionary<double, string>
								{
									{1, "this is 1"},
									{2, "this is 2"},
								},
						Name = "avariablename_02",
						PrintFormat = new OutputFormat(0x00050802),
						WriteFormat = new OutputFormat(0x00050802),
						Type = DataType.Numeric,
						Width = 10,
						MissingValues = new List<double>{999},
					};

				var variables = new List<Variable>
					{
						variable1,
						variable2
					};

				var options = new SpssOptions();

				using (var writer = new SpssWriter(fileStream, variables, options))
				{ 
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
        }

		[TestMethod]
		public void TestWriteString()
		{
			var filename = @"C:\Tests\testWriteString.sav";

			using (FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
			{

				var variable1 = new Variable
				{
					Label = "The variable Label",
					ValueLabels = new Dictionary<double, string>
							{
								{1, "Label for 1"},
								{2, "Label for 2"},
							},
					Name = "avariablename_01",
					PrintFormat = new OutputFormat(0x00050802), // TODO improve the output format creation
					WriteFormat = new OutputFormat(0x00050802),
					Type = DataType.Numeric,
					Width = 10,
					MissingValues = new List<double> { 999 },
				};

				var varString = new Variable
				{
					Label = "This is a string variable",
					Name = "stringvar_01",
					PrintFormat = new OutputFormat(FormatType.A, 60), 
					WriteFormat = new OutputFormat(FormatType.A, 60),
					Type = DataType.Text,
					Width = 60,
					TextWidth = 60,
				};

				var variable2 = new Variable
				{
					Label = "Another variable",
					ValueLabels = new Dictionary<double, string>
								{
									{1, "this is 1"},
									{2, "this is 2"},
								},
					Name = "avariablename_02",
					PrintFormat = new OutputFormat(0x00050802),
					WriteFormat = new OutputFormat(0x00050802),
					Type = DataType.Numeric,
					Width = 10,
					MissingValues = new List<double> { 999 },
				};

				var variables = new List<Variable>
					{
						variable1,
						varString,
						variable2
					};

				var options = new SpssOptions();

				using (var writer = new SpssWriter(fileStream, variables, options))
				{
					var newRecord = writer.CreateRecord();
					newRecord[0] = 15d;
					newRecord[1] = "adsf ñlkj";
					newRecord[2] = 15.5d;
					writer.WriteRecord(newRecord);
					
					newRecord = writer.CreateRecord();
					newRecord[0] = 150d;
					newRecord[1] = null;
					newRecord[2] = 150d;
					writer.WriteRecord(newRecord);
					
					newRecord = writer.CreateRecord();
					newRecord[0] = null;
					// 300 chars, should be cut to 60
					newRecord[1] = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor. Aenean massa. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec quam felis, ultricies nec, pellentesque eu, pretium quis, sem. Nulla consequat massa quis enim. Donec.";
					newRecord[2] = 200d;
					writer.WriteRecord(newRecord);
					writer.EndFile();
				}
			}
		}
    }
}
