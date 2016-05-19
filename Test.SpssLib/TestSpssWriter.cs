using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpssLib.DataReader;
using SpssLib.SpssDataset;

namespace Test.SpssLib
{
    [TestClass]
    public class TestSpssWriter
    {
        [TestMethod]
        public void TestWriteNumbers()
        {
            var filename = @"testWriteNumbers.sav";

            var variables = new List<Variable>
                            {
                                new Variable("avariablename_01")
                                {
                                    Label = "The variable Label",
                                    ValueLabels = new Dictionary<double, string>
                                                  {
                                                      {1, "Label for 1"},
                                                      {2, "Label for 2"},
                                                  },
                                    PrintFormat = new OutputFormat(FormatType.F, 8, 2),
                                    WriteFormat = new OutputFormat(FormatType.F, 8, 2),
                                    Type = DataType.Numeric,
                                    Width = 10,
                                    MissingValueType = MissingValueType.OneDiscreteMissingValue
                                },
                                new Variable("avariablename_02")
                                {
                                    Label = "Another variable",
                                    ValueLabels = new Dictionary<double, string>
                                                  {
                                                      {1, "this is 1"},
                                                      {2, "this is 2"},
                                                  },
                                    PrintFormat = new OutputFormat(FormatType.F, 8, 2),
                                    WriteFormat = new OutputFormat(FormatType.F, 8, 2),
                                    Type = DataType.Numeric,
                                    Width = 10,
                                    MissingValueType = MissingValueType.OneDiscreteMissingValue
                                }
                            };
            variables[0].MissingValues[0] = 999;
            variables[1].MissingValues[0] = 999;

            var options = new SpssOptions();

            using (FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
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

            int varCount;
            int rowCount;
            ReadFile(filename, out varCount, out rowCount);

            Assert.AreEqual(varCount, 2, "Variable count does not match");
            Assert.AreEqual(rowCount, 2, "Rows count does not match");
        }

        [TestMethod]
        public void TestWriteString()
        {
            var filename = @"testWriteString.sav";

            var varString1 = new Variable("stringvar_01")
                             {
                                 Label = "This is a string variable",
                                 Type = DataType.Text,
                                 TextWidth = 500,
                             };

            var variable1 = new Variable("avariablename_01")
                            {
                                Label = "The variable Label",
                                ValueLabels = new Dictionary<double, string>
                                              {
                                                  {1, "Label for 1"},
                                                  {2, "Label for 2"},
                                              },
                                PrintFormat = new OutputFormat(FormatType.F, 8, 2),
                                WriteFormat = new OutputFormat(FormatType.F, 8, 2),
                                Type = DataType.Numeric,
                                Width = 10,
                                MissingValueType = MissingValueType.OneDiscreteMissingValue,
                                MissingValues = {[0] = 999}
                            };

            var varString = new Variable("stringvar_02")
                            {
                                Label = "This is a string variable",
                                PrintFormat = new OutputFormat(FormatType.A, 60),
                                WriteFormat = new OutputFormat(FormatType.A, 60),
                                Type = DataType.Text,
                                Width = 60,
                                TextWidth = 60,
                            };

            var variable2 = new Variable("avariablename_02")
                            {
                                Label = "Another variable",
                                ValueLabels = new Dictionary<double, string>
                                              {
                                                  {1, "this is 1"},
                                                  {2, "this is 2"},
                                              },
                                PrintFormat = new OutputFormat(FormatType.F, 8, 2),
                                WriteFormat = new OutputFormat(FormatType.F, 8, 2),
                                Type = DataType.Numeric,
                                Width = 10,
                                MissingValueType = MissingValueType.OneDiscreteMissingValue,
                                MissingValues = {[0] = 999}
                            };

            var variables = new List<Variable>
                            {
                                varString1,
                                variable1,
                                varString,
                                variable2
                            };

            var options = new SpssOptions();

            var data = new[]
                       {
                           new object[]
                           {
                               // Exactly 500
                               "Lorem ipsum dolor sit amet, consectetur adipiscing elit. In iaculis neque eget neque semper, vitae lacinia libero rhoncus. Mauris tellus lorem, imperdiet vitae bibendum ac, rhoncus nec dui. Donec in consequat leo. Nunc at ante nec metus aliquam hendrerit quis a augue. Suspendisse faucibus nunc mauris, sed faucibus mauris bibendum et. Sed auctor, dolor non luctus interdum, tellus neque auctor dui, sit amet luctus neque risus vel nibh. Nullam ornare ultricies quam. Vestibulum eget erat sit nullam.",
                               15d,
                               "adsf ñlkj",
                               15.5d
                           },
                           new object[]
                           {
                               // 600, should be cut to 500
                               "Lorem ipsum dolor sit amet, consectetur adipiscing elit. In elementum sed justo eu pulvinar. Maecenas non laoreet justo, eget ultrices dolor. Praesent sit amet sodales erat. Proin condimentum, metus et pulvinar ultrices, massa erat hendrerit lorem, vel mattis dolor ante id sem. Fusce laoreet mi tortor, ut interdum ipsum laoreet vel. Nullam lorem mauris, vulputate luctus velit placerat, scelerisque vehicula elit. Phasellus gravida ante quis augue convallis venenatis. Integer bibendum purus non felis interdum, quis fermentum tellus sodales. Fusce commodo ultrices leo ut vulputate. Quisque metus.",
                               150d,
                               null,
                               150d
                           },
                           new object[]
                           {
                               // 255 chars
                               "Lorem ipsum dolor sit amet, consectetur adipiscing elit. In iaculis neque eget neque semper, vitae lacinia libero rhoncus. Mauris tellus lorem, imperdiet vitae bibendum ac, rhoncus nec dui. Donec in consequat leo. Nunc at ante nec metus aliquam hendrerit_",
                               null,
                               // 300 chars, should be cut to 60
                               "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor. Aenean massa. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec quam felis, ultricies nec, pellentesque eu, pretium quis, sem. Nulla consequat massa quis enim. Donec.",
                               200d
                           }
                       };

            using (FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new SpssWriter(fileStream, variables, options))
                {
                    // If the data record is not readly available, you can use: object[] newRecord = writer.CreateRecord();
                    foreach (var row in data)
                    {
                        writer.WriteRecord(row);
                    }
                    writer.EndFile();
                }
            }

            int varCount;
            int rowCount;
            ReadFile(filename, out varCount, out rowCount);

            Assert.AreEqual(varCount, 4, "Variable count does not match");
            Assert.AreEqual(rowCount, 3, "Rows count does not match");
        }


        [TestMethod]
        public void TestWriteLongWeirdString()
        {
            var filename = @"testTestWriteLongWeirdString.sav";

            var varString1 = new Variable("stringvar_01")
                             {
                                 Label = "This is a string variable",
                                 Type = DataType.Text,
                                 TextWidth = 5000,
                             };

            var variable1 = new Variable("avariablename_01")
                            {
                                Label = "The variable Label",
                                ValueLabels = new Dictionary<double, string>
                                              {
                                                  {1, "Label for 1"},
                                                  {2, "Label for 2"},
                                              },
                                PrintFormat = new OutputFormat(FormatType.F, 8, 2),
                                WriteFormat = new OutputFormat(FormatType.F, 8, 2),
                                Type = DataType.Numeric,
                                Width = 10,
                                MissingValueType = MissingValueType.OneDiscreteMissingValue,
                                MissingValues = {[0] = 999}
                            };

            var varString = new Variable("stringvar_02")
                            {
                                Label = "This is a string variable",
                                Type = DataType.Text,
                                TextWidth = 60,
                                Alignment = Alignment.Centre,
                                MeasurementType = MeasurementType.Ordinal
                            };

            var variable2 = new Variable("avariablename_02")
                            {
                                Label = "Another variable",
                                ValueLabels = new Dictionary<double, string>
                                              {
                                                  {1, "this is 1"},
                                                  {2, "this is 2"},
                                              },
                                PrintFormat = new OutputFormat(FormatType.F, 8, 2),
                                WriteFormat = new OutputFormat(FormatType.F, 8, 2),
                                Type = DataType.Numeric,
                                Width = 10,
                                MissingValueType = MissingValueType.OneDiscreteMissingValue,
                                MissingValues = {[0] = 999}
                            };

            var variables = new List<Variable>
                            {
                                varString1,
                                variable1,
                                varString,
                                variable2
                            };

            var options = new SpssOptions();

            using (FileStream writeFileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new SpssWriter(writeFileStream, variables, options))
                {
                    var newRecord = writer.CreateRecord();
                    // Exactly 500
                    newRecord[0] =
                        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. In iaculis neque eget neque semper, vitae lacinia libero rhoncus. Mauris tellus lorem, imperdiet vitae bibendum ac, rhoncus nec dui. Donec in consequat leo. Nunc at ante nec metus aliquam hendrerit quis a augue. Suspendisse faucibus nunc mauris, sed faucibus mauris bibendum et. Sed auctor, dolor non luctus interdum, tellus neque auctor dui, sit amet luctus neque risus vel nibh. Nullam ornare ultricies quam. Vestibulum eget erat sit nullam.";
                    newRecord[1] = 15d;
                    newRecord[2] = "L߀ｒêϻ ｉρѕüｍ ԁòɭｏｒ ѕìｔ ρｏѕùｅｒê.";
                    newRecord[3] = 15.5d;
                    writer.WriteRecord(newRecord);

                    newRecord = writer.CreateRecord();

                    newRecord[0] =
                        "Lòｒêｍ ìｐｓúϻ ｄòｌòｒ ѕïｔ àϻｅｔ, ｃ߀ԉｓèｃｔｅｔûｒ ａᏧìｐｉѕｃｉԉｇ ｅｌìｔ. Vëѕｔìƅùɭｕϻ ａｃ ｉｐｓｕｍ ɭëｃｔùѕ. Cｒａѕ ｖìｔａè ѵïѵëｒｒä ｔｏｒｔòｒ. Nｕｎｃ ｖìｔàë ｅѕｔ ｖäｒìúѕ, ɭãｏｒèëｔ ｔòｒｔ߀ｒ á, ïɑｃûｌíѕ ｓãｐíｅｎ. Pｈɑｓêɭɭｕｓ ｍｅｔｕｓ ϻáɢлå, ｆêûɢíàｔ ä ɋùåϻ âɭïｑúáｍ, ѕｃëｌéｒïѕｑüé ｍòｌêѕｔｉè ｓɑｐｉëԉ. Uｔ ｎúлｃ ԉïѕｌ, éｆｆìｃïｔúｒ ｖｅｌ ｃｕｒѕûѕ à, ãɭｉｑûãϻ ѕｉｔ áϻｅｔ úｒｎå. Mａúｒïｓ ｃｏｎԁìϻｅｎｔùｍ ｏｒｎãｒë ｐùｒúｓ, ρհäｒｅｔｒã ｂɭãлԁíｔ ɭê߀ ｃ߀ϻϻòᏧ߀ ｅû. Mãúｒìｓ éｇëｔ ｍäѕѕä ｎèｃ ѕéϻ ｖòｌúｔρɑｔ ｅｌëϻｅｎｔúϻ ɋûｉｓ ɑ ｄùï. Cｕｒåƅìｔûｒ ｉԉｔｅｒᏧùϻ ｐùｌѵíԉàｒ ｆèɭïѕ, èú ｆåｃïｌíｓｉｓ ɭëｃｔûѕ ԁàρìƅüｓ ｓｉｔ àϻéｔ. Cｒãѕ ｔéｍｐüｓ ëｌëϻéлｔüｍ ｍëｔùｓ. Fúｓｃｅ ëх íρｓûϻ, ｆｒíԉｇíｌɭâ àｔ ｌéｃｔùｓ éｔ, ｌùｃｔùѕ ｐɭɑｃéｒａｔ ρùｒûѕ. Måéｃｅлａѕ ｎｅｃ ｌèò ɭｅò. Víѵàϻｕｓ ѵüɭｐúｔɑｔë ｌìƅèｒｏ êɢêｔ ѵ߀ɭúｔｐａｔ ѵòɭùｔｐàｔ. Aëлèàԉ âｃ ｄïｃｔúｍ ｍëｔｕѕ, лêｃ ｃ߀лｄìｍëлｔｕｍ áԉｔｅ. M߀ｒｂï ɭãｃüѕ ｒíѕûｓ, ｉｍρëｒᏧíêｔ ｖｏｌüｔｐáｔ ｌéｏ л߀л, ｌúｃｔùｓ ｔëｍｐúｓ ߀ｒｃí. D߀ԉêｃ äｃ ａɭｉｑｕɑｍ ｅｌｉｔ. Cúｒáƅíｔｕｒ ｔíｎｃíԁúｎｔ åԉｔé ϻｏｌｌíｓ, ѕòԁáｌëѕ ëｌｉｔ ëǥèｔ, ｐëɭｌéｎｔèѕɋûｅ äùｇúé. Nｕɭɭâ èｔ éХ ｆèɭìｓ. Fｕｓｃë ɭｏƅｏｒｔìｓ ｌ߀ｒｅϻ ԉｅｃ ｎｅｑûｅ ùｌｔｒíｃéｓ, á ƃɭâԉｄíｔ ｔùｒρïѕ ｃòԉѕèɋùäｔ. Séԁ èü ｃüｒｓûｓ åüｇúè, ｉᏧ ｃ߀лѕëɋｕãｔ Ꮷｏɭ߀ｒ. Sｅԁ ɑｃｃｕｍｓâԉ, ｄｏɭ߀ｒ ｉԉｔｅｒｄüｍ ｍòɭëѕｔíê åｌｉｑúèｔ, ｒｉѕｕｓ ԉìｓɭ ｉåｃùｌïｓ áｎｔè, ԉ߀л ԁïǥԉíｓｓíϻ ϻâɢｎå ԉìｂɦ ɑｔ äｒｃú. Uｔ éｆｆïｃｉｔûｒ éｔ ԁìàϻ ｖｉｔáê ԁìｃｔùϻ. Eｔｉａϻ üｔ ɑüｃｔ߀ｒ ｎïƃｈ. Iлｔêɢéｒ èｒâｔ ｓâρｉéԉ, ｐｒèｔìｕϻ á êｓｔ ԉëｃ, ｏｒｎɑｒê ｌûｃｔûѕ лìｓｌ. Pêｌɭëԉｔèѕɋｕë ϻàｌｅｓüáԁａ ｄíｃｔûｍ èԉíｍ íԁ áɭìｑüèｔ. SｕｓρêԉᏧｉѕｓｅ ｓíｔ ãｍëｔ ߀ｒｎåｒｅ ｒïѕｕｓ, ｖｅｌ íáｃùɭìｓ ｌɑｃúѕ. Pհäѕｅｌｌûｓ յûｓｔｏ ｍèｔúѕ, ｍäｘïϻûѕ âｃ ρｒｅｔíｕϻ ѵêｌ, ｓãｇìｔｔìѕ ｆａúｃｉƃｕѕ ｌ߀ｒêϻ. Vïѵãϻüｓ ｑúïｓ ｃｏｎѵäɭｌｉｓ ϻï, ɋｕïｓ ｆèùɢïåｔ лìｓì. Pｈáѕêɭɭûѕ ｔｒïѕｔｉｑùê ɭɑｃûѕ äｃ ϻòɭｅｓｔïé ｍäｌéｓúａｄâ. Qûìｓｑûê ρｒëｔìｕｍ, ｎíｂｈ êｇéｔ ｒüｔｒùϻ ѕｏｌɭïｃｉｔüᏧíԉ, ｍéｔûѕ ｌïƃëｒ߀ ｆáüｃíƅｕѕ ɭｅｃｔúѕ, éｌｅｍèԉｔüϻ ρｏｒｔɑ ɭïǥûｌɑ ｄｏｌ߀ｒ ｐûｌѵïлɑｒ èｒ߀ｓ. Aéԉêɑл ｎèｃ ｒüｔｒûϻ ｐûｒｕѕ. Nɑｍ ｖèｌ ԉìｓí ϻàｇｎà. Cùｒäƃìｔｕｒ ｕｔ ρ߀ｓûèｒè յüѕｔò. Sèԁ ｅｆｆìｃìｔûｒ ｐ߀ｒｔｔïｔòｒ ｄïäϻ, ä ｆèûｇíâｔ ｖëɭíｔ ïԉｔéｒᏧüｍ éü. SêᏧ ｕｔ ｃòлѕéｑúａｔ ɭãｃûｓ. Iԉ ïｎ ｃ߀ԉᏧïϻéлｔｕｍ ｔûｒｐìｓ, ｓìｔ ａϻｅｔ ｃ߀ｎｇûé ｅｒäｔ. Qùìѕɋûｅ ƃɭàԉᏧｉｔ ｌàｃùѕ ｉϻｐêｒᏧìéｔ ѕàｐíｅл ƅíƅéｎｄüｍ ԁìɢｎｉѕｓíｍ. Mòｒｂｉ ｂïｂèлｄüｍ ѵ߀ｌùｔρäｔ ԉíѕｌ. Sëｄ ùｌｔｒïｃíｅѕ ｔｅɭｌûѕ ｎòԉ ɋûäｍ âûｃｔòｒ, ｓｅᏧ ρɭａｃèｒâｔ ｌìｇûｌɑ ｔｅｍρòｒ. Mòｒƃì ｉｍρéｒᏧｉèｔ ｃｏԉǥùé ｑûáϻ, ïᏧ ｐｕｌѵíлａｒ ԁüｉ. Péｌɭêԉｔêѕｑúë ｍòｌêｓｔïè êХ ｔ߀ｒｔｏｒ. Mäüｒìｓ ѵìｔâê ｒïѕùｓ ìｄ ߀ｒｃï ｍâｔｔｉｓ ｆｉлïƅüѕ. Dòｎéｃ ｃûｒｓûѕ ｓｏɭｌíｃìｔｕԁïԉ лïｓｌ ａｔ ｓêϻｐëｒ. Dｏлèｃ ɦêｎｄｒëｒｉｔ ɭêｃｔüѕ á ｌíｂｅｒò ϻɑɭèѕｕãｄâ, ѕｉｔ ɑϻéｔ ϻáӽìｍüѕ ｊùѕｔ߀ üｌｔｒｉｃêѕ. Vëｓｔïƃùｌûｍ ɭｉƅｅｒò ｎíｓí, ｖèｓｔïƃûｌｕｍ äｃ ｃｏԉѕêｑúãｔ åｃ, ｃòԉѕèｃｔêｔûｒ л߀ԉ ｅｒｏｓ. Mäûｒｉｓ åɭíɋûåｍ àｒｃü ｃùｒѕûѕ, ｈｅԉᏧｒëｒïｔ ìρｓùϻ èü, ѵëɦïｃüɭá ｆèɭìｓ. Pｈàｓèｌｌûｓ ρｏｓｕèｒｅ ｔｅɭɭúｓ ｅɢèｔ ɭïǥúｌã ｍåｔｔïѕ, ѕｉｔ àｍéｔ ｖｉѵｅｒｒå ùｒｎɑ ãｌｉɋûêｔ. Péｌｌｅлｔëѕｑûë ìԁ ｔòｒｔｏｒ ａлｔè. Aｅлèãԉ ѕ߀ｌｌｉｃìｔüᏧïｎ ｖéɭｉｔ ìᏧ ｐëｌɭｅԉｔëｓɋｕé éùïｓϻ߀ԁ. Nùɭɭâ ｇｒáѵìｄá ｔìԉｃｉԁúԉｔ ùｒлá, ѕëԁ ｃòлԁïｍéлｔûｍ ｍａùｒíѕ ｔｒìｓｔíɋúé ｕｔ. Cｒäѕ ｍɑｔｔïѕ ｆêɭïｓ ɋûïｓ ｃｕｒｓｕｓ íϻρｅｒԁｉｅｔ. Iлｔｅｇèｒ ｃ߀лｇùë ｌåｃìлｉａ ｔûｒｐíｓ ùｔ ѵｅհïｃｕɭａ. Dùíｓ ｌëｃｔｕｓ úｒｎå, ѕéϻｐｅｒ å ѵíѵëｒｒà ïᏧ, ϻãｌｅѕｕâᏧå ûｔ ëԉïｍ. Aｅԉêãԉ ｒïｓｕѕ êｒäｔ, èǥéｓｔâｓ ａ ｌ߀ƃòｒｔíｓ íｎ, ɑɭíɋûëｔ ｆｉｎïƅùѕ ｓëｍ. Vëѕｔïƃｕｌúϻ հëԉԁｒéｒïｔ éԉｉｍ ԉèｃ äùｃｔｏｒ Ꮷïǥлìѕѕíｍ. Nａｍ ìàｃúɭïｓ ɭäｃùѕ ｅǥéｔ ԁｕｉ ｏｒлáｒｅ ïãｃûɭïｓ. Uｔ íᏧ ѵâｒïúѕ лëｑúè, êｇéｔ ｔëϻｐ߀ｒ ｆèｌｉѕ. Sêｄ ρòｒｔｔｉｔｏｒ ѵíѵéｒｒà ｌéò ｓíｔ åｍèｔ ϻ߀ɭɭïｓ. Núｌｌä ｃｏｍϻ߀ԁ߀ èｆｆíｃìｔûｒ ԉïѕｌ. Dｏｎｅｃ ｖâｒìüｓ ԉｕｌɭá ѕïｔ ɑϻéｔ ѕàｐìëл ԁｉɢлíｓѕíϻ, ìԉ ｕɭɭɑｍｃｏｒｐｅｒ òᏧíｏ ｓｅϻρéｒ. M߀ｒƃｉ ｃ߀ｍϻｏԁ߀ ｖｅɭïｔ ùｔ ｃ߀лᏧíϻｅԉｔûｍ ｔíｎｃìԁùлｔ. Vｅѕｔïｂüｌûｍ ｃｕｒｓüѕ ｄｉｃｔûϻ ｆêｌïѕ ｖëɭ ｆìлíƃｕｓ. Cｌａｓѕ àｐｔêｎｔ ｔåｃìｔí ѕòｃïòｓｑü ãᏧ ｌíｔ߀ｒａ ｔｏｒｑｕｅｎｔ ｐêｒ ｃ߀ｎüƃíɑ ｎｏｓｔｒâ, ｐｅｒ ìｎｃèｐｔ߀ѕ ｈｉϻｅԉáé߀ѕ. Sｕｓρéｎｄïѕｓé ûｔ ԉüлｃ ｕɭｔｒｉｃêѕ ｍａǥԉã ｖúɭｐûｔäｔë ｐｈａｒéｔｒɑ ｎｏｎ ｌâｃíｎïâ ｄïâϻ. Pèɭｌｅԉｔéѕɋúｅ ǥｒàѵíｄã ｓｏｄâｌéѕ ｓèｍ, ëｔ ｃｏԉｇûë лùｌｌà ｃ߀ｎѕèｃｔëｔúｒ ɋûìｓ. Cúｒãƃíｔûｒ ｓùѕｃïρïｔ àｒｃù ｒíｓùѕ, ԉｏл ｃòϻｍｏᏧｏ ɋùâｍ êɭｅíｆéԉԁ ѵêɭ. Mａêｃéԉàѕ ｍ߀ɭɭíѕ ｎúɭｌä àｃ ɭòｂòｒｔíｓ ｆïлïƃüｓ. Vｉｖãϻúѕ лｅｃ äԉｔｅ ѕâρíéԉ. Sëԁ ｓｃéｌëｒíｓｑüｅ ｍáｇｎａ ѕëｍ, ëǥèｔ ｖｏｌｕｔｐäｔ êｘ ｔｒíｓｔｉｑùê ïԁ. Iｎｔèɢｅｒ ｅｌéｉｆëｎԁ ɭɑòｒêｅｔ ｔòｒｔｏｒ, èｔ ѕｏｄａɭèｓ лûɭｌá ƃｌɑԉｄｉｔ ｓｅᏧ. Qùïѕɋúê ｅü ρùｒûｓ ѵòｌüｔｐａｔ, ｍａɭêｓüａᏧá ｏｄïò ｎëｃ, ｃ߀ｎԁïϻëԉｔûｍ ｎìｓｉ. Vêｓｔìƅùɭùϻ ѕ߀ɭɭｉｃìｔùｄíԉ ｅｒòѕ ëｇｅｔ úｌｌａｍｃòｒρｅｒ ｐｏѕｕｅｒｅ. Iｎｔêｇéｒ êｆｆíｃíｔûｒ åúｃｔｏｒ ａｌïｑûèｔ. Nｕԉｃ ѕｃêｌèｒíｓｑüｅ ѕïｔ âｍëｔ èɭìｔ ëｇéｔ ｒúｔｒúｍ. Péɭɭëｎｔêｓɋｕë ｖíｔâê ｐｒèｔｉüｍ ϻëｔüｓ, ａｔ ａｕｃｔｏｒ êӽ. Qüｉѕɋùê ϻëｔüｓ ｍâｇлａ, Ꮷàρｉƅｕѕ àｃ ｓéϻ ùｔ, ɭàｃｉԉｉɑ ｐհɑｒｅｔｒɑ ɋｕàϻ. Pëɭɭêлｔëｓɋüé ëúìｓｍ߀ԁ ɭòƅｏｒｔｉｓ ѵèｌｉｔ, ìԁ ｓｃèɭéｒｉｓｑｕè ｑúａϻ áｃｃüϻｓãｎ èü. Sｅｄ íｎ ｌｉｂêｒò ɭｕｃｔùｓ, ѕêｍρèｒ ｌè߀ ａｔ, ϻɑｔｔìѕ ɭéｃｔüｓ. Dｕïѕ ｅｌｅｍéлｔúｍ, ｌëｃｔüѕ ѕëԁ ｓèϻｐéｒ ｃｏԉｖâɭｌïѕ, Ꮷûí ëｘ ùｌｌɑϻｃ߀ｒρêｒ ｍɑúｒｉѕ, ｖëɭ ｓüｓｃïρíｔ éԉïϻ èｒ߀ѕ ｕｌｔｒïｃèｓ ѕàρïéл. Nûԉｃ éｆｆìｃïｔûｒ ｌéò ëù ｖìѵéｒｒá ｍｏɭｌíѕ. Eｔìäϻ Ꮷáρïｂüｓ ɭｏｂòｒｔïｓ ѵｕɭρüｔáｔë. Pèｌɭèｎｔëｓɋùê ａｔ ｃｏｍｍｏԁò íρѕúｍ. Düíѕ ｔéϻｐûｓ ѵéհïｃúɭɑ êｒɑｔ ɭｕｃｔüｓ ëɭëïｆëлԁ. Sｅｄ íｄ ｖëｓｔｉｂùｌｕｍ ùｒԉɑ. Fｕｓｃê ｒｕｔｒûϻ, ｖêｌíｔ äｃ ｔèϻρｏｒ ｍåｌéѕûãᏧá, ｔéｌɭüｓ лìѕí ｆåúｃìƅｕѕ ϻåǥԉａ, ѵìｔａé ϻãｔｔìѕ Ꮷíɑｍ ɭêｃｔｕѕ ｖïｔàê ߀ｒｃï. Péｌｌｅｎｔｅｓｑûè ãｌｉｑúａｍ ｃｏｎｓèｃｔｅｔûｒ ëɢｅѕｔãѕ. Dòԉｅｃ ïԁ ïåｃûｌíｓ ｄúｉ. Nûɭｌáϻ ɭｏƅ߀ｒｔìｓ ｎìѕï ëǥèｔ Ꮷüｉ ëｕïｓｍòԁ ｐɭａｃëｒáｔ. Fûｓｃë ｂìｂéԉԁúｍ, ɭìɢûｌá ѵëｌ ѵêɦïｃùɭå ｐ߀ѕùëｒｅ, лｕɭɭａ ϻãѕѕå ɭａｃíｎíａ ɭïǥｕｌａ, ïԉ ｓ߀ｌｌíｃìｔüｄíｎ ｅｒａｔ ｔùｒｐíｓ éü ìｐｓûϻ. Nüԉｃ ｃｏｎԁｉｍｅлｔûϻ ｍäɭéｓúáԁå ｌàｃúѕ. Dｏԉéｃ ãｃ ｍäｌêѕüäᏧã âüǥùｅ, ìԉ ɭäｃìлｉá ɭｉｇüɭâ. Núɭɭɑϻ ëѕｔ ãｎｔè, úｌｌáϻｃòｒρêｒ ｃ߀лｓêɋúåｔ ｄｉáｍ лòｎ, ëɢêѕｔàｓ ãɭïɋｕâϻ ԉｕлｃ. Pèɭɭëлｔｅｓɋúê ｈáƃïｔäｎｔ ϻｏｒｂì ｔｒïｓｔíｑùé ѕèԉｅｃｔｕｓ êｔ лｅｔｕѕ ëｔ ｍａｌéѕûａᏧä ｆɑｍëｓ åｃ ｔùｒρïѕ êɢêѕｔãѕ. Nüｌɭä ｆɑｃìɭｉѕï. Måüｒｉｓ ｔìлｃìｄúｎｔ ｉｐѕúϻ ɭ߀ｒéｍ, ѕëᏧ ѵｉѵëｒｒã èлíｍ ѕｅϻｐêｒ ûｔ. Aêлëäԉ ｃｏлｖáɭɭｉｓ ρèɭɭéԉｔèѕｑúê ｑûàｍ, ѕéԁ ƅɭɑԉԁｉｔ Ꮷïâϻ ｖéɦïｃｕɭａ ｍéｔｕѕ.";
                    newRecord[1] = 150d;
                    newRecord[2] = null;
                    newRecord[3] = 150d;
                    writer.WriteRecord(newRecord);

                    newRecord = writer.CreateRecord();

                    newRecord[0] =
                        "Lòｒèｍ ｉｐѕüϻ Ꮷòｌｏｒ ѕíｔ áｍｅｔ, ｃｏԉѕéｃｔëｔùｒ ãԁｉρìｓｃíлǥ êｌìｔ. Iл ïａｃûｌｉｓ ԉêｑûë êｇéｔ ｎèɋùé ѕêｍｐëｒ, ｖíｔɑｅ ｌäｃïлíâ ɭíｂèｒ߀ ｒｈｏｎｃüѕ. Mâûｒíѕ ｔèｌｌｕｓ ｌｏｒéｍ, ìｍρéｒԁｉëｔ ѵìｔɑｅ ƃïƅèԉｄúｍ áｃ, ｒɦòｎｃûｓ ԉéｃ ｄùｉ. D߀ｎëｃ ïｎ ｃ߀лｓéɋûãｔ ｌèｏ. Núлｃ àｔ äｎｔｅ ԉｅｃ ϻêｔüѕ åɭïｑûåｍ ｈｅｎԁｒéｒíｔ ｑüíｓ á âüǥûｅ. SûｓρëԉᏧíｓѕê ｆåúｃïｂüｓ лｕｎｃ ｍäûｒｉѕ, ѕｅᏧ ｆàùｃíƅüѕ ｍâúｒíѕ ƅïｂêｎｄúｍ èｔ. SêᏧ ãûｃｔｏｒ, Ꮷòɭｏｒ ｎ߀ｎ ｌüｃｔｕѕ ïлｔｅｒｄùｍ, ｔéｌｌｕｓ ｎéｑúë àûｃｔ߀ｒ ԁｕì, ѕìｔ áｍëｔ ɭùｃｔｕѕ ԉｅɋùé ｒíѕûｓ ｖｅɭ ｎｉƃɦ. Nüｌｌａϻ ߀ｒԉåｒé üɭｔｒíｃìëｓ ｑùãｍ. Vèѕｔïｂｕｌｕｍ ëɢｅｔ ｅｒɑｔ ѕïｔ ԉúɭɭãｍ.";
                    newRecord[1] = null;
                    newRecord[2] = "Lorem ipsum dolor sit posuere.";
                    newRecord[3] = 200d;
                    writer.WriteRecord(newRecord);
                    writer.EndFile();
                }
            }

            int varCount;
            int rowCount;
            ReadFile(filename, out varCount, out rowCount);

            Assert.AreEqual(varCount, 4, "Variable count does not match");
            Assert.AreEqual(rowCount, 3, "Rows count does not match");
        }

        [ExpectedException(typeof (ArgumentNullException))]
        [TestMethod]
        public void TestNullVarnameException()
        {
            var filename = @"thisFileShouldNotExist.sav";

            var variables = new List<Variable>
                            {
                                new Variable(null)
                            };

            using (FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                using (new SpssWriter(fileStream, variables))
                {
                }
            }
        }

        [ExpectedException(typeof (ArgumentException))]
        [TestMethod]
        public void TestNoVarnameException()
        {
            var filename = @"emptyErrorFile.sav";

            var variables = new List<Variable>
                            {
#pragma warning disable 618
                                new Variable()
#pragma warning restore 618
                            };

            using (FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                using (new SpssWriter(fileStream, variables))
                {
                }
            }
        }

        [TestMethod]
        public void TestWriteDoubleLongStringVar()
        {
            var filename = @"c:\temp\testWriteDoubleLongString.sav";

            using (FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                var var1 = new Variable("stringvar_01")
                                 {
                                     Label = "This is a string variable",
                                     Type = DataType.Text,
                                     Width = 50,
                                     TextWidth = 5000
                                 };

                var var2 = new Variable("stringvar_02")
                                {
                                    Label = "This is a string variable",
                                    PrintFormat = new OutputFormat(FormatType.A, 60),
                                    WriteFormat = new OutputFormat(FormatType.A, 70),
                                    Type = DataType.Text,
                                    Width = 50,
                                    TextWidth = 5000
                                };

                var var3 = new Variable("stringvar_03")
                                 {
                                     Label = "This is a string variable",
                                     PrintFormat = new OutputFormat(FormatType.A, 60),
                                     WriteFormat = new OutputFormat(FormatType.A, 70),
                                     Type = DataType.Text,
                                     Width = 50,
                                     TextWidth = 5000
                                 };                

                var var4 = new Variable("stringvar_04")
                {
                    Label = "This is a string variable",
                    Type = DataType.Text,
                    Width = 50,
                    TextWidth = 50
                };

                var var5 = new Variable("stringvar_05")
                {
                    Label = "This is a string variable",
                    Type = DataType.Text,
                    Width = 4,
                    TextWidth = 4
                };

                var var6 = new Variable("LIKES")
                {
                    Label = "This is a string variable",
                    PrintFormat = new OutputFormat(FormatType.A, 255),
                    WriteFormat = new OutputFormat(FormatType.A, 255),
                    Type = DataType.Text,
                    Width = 50,
                    TextWidth = 1026
                };


                var variables = new List<Variable>
                                {
                                    var1,
                                    var2,
                                    var3,
                                    var4,
                                    var5,
                                    var6
                                };

                var options = new SpssOptions();

                using (var writer = new SpssWriter(fileStream, variables, options))
                {
                    object[] newRecord = writer.CreateRecord();
                    newRecord[0] = "test";
                    newRecord[1] = "test";
                    newRecord[2] = "Lorem";
                    //newRecord[3] = 200d;
                    writer.WriteRecord(newRecord);
                    writer.EndFile();
                }
            }
        }

        private static void ReadFile(string filename, out int varCount, out int rowCount)
        {
            FileStream readFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read,
                FileShare.Read, 2048*10, FileOptions.SequentialScan);

            try
            {
                TestSpssReader.ReadData(readFileStream, out varCount, out rowCount);
            }
            finally
            {
                readFileStream.Close();
            }
        }
    }
}