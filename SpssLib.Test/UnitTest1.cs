using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SpssLib.Test
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestReadMetadata()
		{
			//var filename = @"D:\Projekt\TestProjects\spsslib-80132\Version 0.2 alpha\SpssLib\Test.SpssLib\data\Kjaer.sav";
			//var filename = @"D:\Temp\TRIM EXAMPLES SPSS von Matthias\rational.sav";
			//var filename = @"C:\SourceCode\TRIM\TNS.TRIM - Development\TRIM RC\doc\Migration\SpssSetupDataForImport\Unicode_mit_Wave.sav";
			var filename = @"C:\Users\francisco.biagi\Documents\datasets\Tests\VLS.sav";
			//var filename = @"D:\Temp\TRIM EXAMPLES SPSS von Matthias\rational_pspp.sav";
			FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
			SpssDataset.SpssDataset spssDataset = new SpssDataset.SpssDataset(fileStream);
			fileStream.Close();
			//fileStream.Close();
			var variables = spssDataset.Variables;
			foreach (var variable in variables)
			{
				Console.WriteLine("{0} - {1}", variable.Name, variable.Label);
				foreach (KeyValuePair<double, string> label in variable.ValueLabels)
				{
					Console.WriteLine(" {0} - {1}", label.Key, label.Value);
				}
			}

		}
	}
}
