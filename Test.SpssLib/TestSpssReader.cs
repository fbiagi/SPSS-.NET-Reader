using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using SpssLib.FileParser.Records;
using SpssLib.SpssDataset;

namespace Test.SpssLib
{
    [TestClass]
    public class TestSpssReader
    {

        [TestMethod]
        public void TestReadMetadata()
        {
            //var filename = @"D:\Projekt\TestProjects\spsslib-80132\Version 0.2 alpha\SpssLib\Test.SpssLib\data\Kjaer.sav";
            //var filename = @"D:\Temp\TRIM EXAMPLES SPSS von Matthias\rational.sav";
            //var filename = @"C:\SourceCode\TRIM\TNS.TRIM - Development\TRIM RC\doc\Migration\SpssSetupDataForImport\Unicode_mit_Wave.sav";
			//var filename = @"C:\Users\francisco.biagi\Documents\datasets\Tests\VLS.sav";
            var filename = @"C:\Users\ttbiagif\Documents\Datasets\Clean_dataset.enriched_noflinfo.sav";
            FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            SpssDataset spssDataset = new SpssDataset(fileStream);
            fileStream.Close();
            //fileStream.Close();
            var variables = spssDataset.Variables;
            foreach (var variable in variables)
            {
                Console.WriteLine("{0} - {1}",variable.Name, variable.Label);
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
