using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SpssLib.FileParser.Records;
using SpssLib.SpssDataset;

namespace Test.SpssLib
{
    [TestFixture]
    public class TestSpssReader
    {

        [Test]
        public void TestReadMetadata()
        {
            //var filename = @"D:\Projekt\TestProjects\spsslib-80132\Version 0.2 alpha\SpssLib\Test.SpssLib\data\Kjaer.sav";
            //var filename = @"D:\Temp\TRIM EXAMPLES SPSS von Matthias\rational.sav";
            //var filename = @"C:\SourceCode\TRIM\TNS.TRIM - Development\TRIM RC\doc\Migration\SpssSetupDataForImport\Unicode_mit_Wave.sav";
            var filename = @"C:\Users\ttbiagif\Documents\Datasets\c\Software_Nandos_20140618.sav";
            //var filename = @"D:\Temp\TRIM EXAMPLES SPSS von Matthias\rational_pspp.sav";
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

        }

        [Test]
        public void TestRoundUp()
        {
            var test = Common.RoundUp(0, 4);
            Console.WriteLine(test);
        }


        [Test]
        public void TestReadData()
        {
            var filename = @"D:\Projekt\TestProjects\spsslib-80132\Version 0.2 alpha\SpssLib\Test.SpssLib\data\Kjaer.sav";
            FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            SpssDataset spssDataset = new SpssDataset(fileStream);
            fileStream.Close();
            var records = spssDataset.Records;
            var numberOfRecordsets = records.Count;

            foreach (Record record in records)
            {
              
                foreach (var variable  in spssDataset.Variables)
                {
                    Console.WriteLine("{0}: {1}", variable.Name, record[variable]);
                }
            }
            
        }
    }
}
