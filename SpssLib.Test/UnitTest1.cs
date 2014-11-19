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
			var filename = @"C:\Users\francisco.biagi\Documents\datasets\Tests\weirdLabel.sav";
			
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

			foreach (var record in spssDataset.Records)
			{
				foreach (var variable in variables)
				{
					Console.Write(record.GetValue(variable));
					Console.Write('\t');
				}
				Console.WriteLine();
			}
		}
	}
}
