using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpssLib.DataReader;

namespace Test.SpssLib
{
    [TestClass]
    public class TestSpssReader
    {

        [TestMethod]
        [DeploymentItem(@"TestFiles\test.sav")]
        public void TestReadMetadata()
        {
            FileStream fileStream = new FileStream("test.sav", FileMode.Open, FileAccess.Read, 
                FileShare.Read, 2048*10, FileOptions.SequentialScan);


            int varCount;
            int rowCount;
            try
            {
                ReadData(fileStream, out varCount, out rowCount);
            }
            finally
            {
                fileStream.Close();
            }

            Assert.AreEqual(varCount, 3, 0, "Variable count does not match");
            Assert.AreEqual(rowCount, 3, 0, "Rows count does not match");
        }

        internal static void ReadData(FileStream fileStream, out int varCount, out int rowCount)
        {
            SpssReader spssDataset = new SpssReader(fileStream);

            varCount = 0;
            rowCount = 0;

            var variables = spssDataset.Variables;
            foreach (var variable in variables)
            {
                varCount++;
                Debug.WriteLine("{0} - {1}", variable.Name, variable.Label);
                foreach (KeyValuePair<double, string> label in variable.ValueLabels)
                {
                    Debug.WriteLine(" {0} - {1}", label.Key, label.Value);
                }
            }

            foreach (var record in spssDataset.Records)
            {
                rowCount++;
                var cellCount = 0;
                foreach (var variable in variables)
                {
                    cellCount++;
                    Debug.Write(variable.Name);
                    Debug.Write(':');
                    Debug.Write(record.GetValue(variable));
                    Debug.Write('\t');
                }
                Debug.WriteLine("");
                Assert.AreEqual(varCount, cellCount, "Row's cell count does not match variable count");
            }
        }
    }
}
