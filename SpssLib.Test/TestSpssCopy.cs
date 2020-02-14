using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpssLib.DataReader;

namespace Test.SpssLib
{
    [TestClass]
    public class TestSpssCopy
    {
        [TestMethod]
        [DeploymentItem(@"TestFiles\cakespss1000similarvars.sav")]
        public void TestCopyFile()
        {
            using (FileStream fileStream =
                new FileStream("cakespss1000similarvars.sav", FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read, 2048 * 10, FileOptions.SequentialScan))
            {
                using (FileStream writeStream = new FileStream("ourcake1000similarvars.sav", FileMode.Create, FileAccess.Write))
                {
                    SpssReader spssDataset = new SpssReader(fileStream);

                    SpssWriter spssWriter = new SpssWriter(writeStream, spssDataset.Variables);

                    foreach (var record in spssDataset.Records)
                    {
                        var newRecord = spssWriter.CreateRecord(record);
                        spssWriter.WriteRecord(newRecord);
                    }

                    spssWriter.EndFile();
                }
            }
            Assert.IsTrue(true); // To check errors, set <DeleteDeploymentDirectoryAfterTestRunIsComplete> to False and open the file
        }
    }
}