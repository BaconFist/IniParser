using Microsoft.VisualStudio.TestTools.UnitTesting;
using IniParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniParser.Tests
{
    [TestClass()]
    public class BMyIniTests
    {
        private string exampleINI = @"; last modified 1 April 2001 by John Doe
[test.owner]
name=John Doe
organization=Acme Widgets Inc.

[test.database]
; use IP address in case network name resolution is not working
server=192.0.2.62     
port=143
file=""payroll.dat""";

        private string exampleIniMulitline = @";demo for list
[test.Codes]
script 1=class Foo {
=""  public Foo(){""
=""    // do stuff""
=""  }""
=""}""
script 2=""    stuff in""
=multiple lines";

        private string exampleINIStuff = @"; Comment. Beginning of INI file
 
; empty lines are ignored
 
[test.General]
; This starts a General section
Compiler=FreePascal
; Key Compiler and value FreePascal
";

        [TestMethod()]
        public void BMyCustomDataTest()
        {
            BMyIni Mock = new BMyIni("", "test");

            Assert.IsInstanceOfType(Mock, typeof(BMyIni));

            BMyIni MockC = new BMyIni(exampleIniMulitline, "test");
            Assert.AreEqual("class Foo {\r\n  public Foo(){\r\n    // do stuff\r\n  }\r\n}", MockC.Read("Codes", "script 1"));
            Assert.AreEqual("    stuff in\r\nmultiple lines", MockC.Read("Codes", "script 2"));
        }

        [TestMethod()]
        public void getSerializedTest()
        {
            BMyIni Mock = new BMyIni("", "test");
            Mock.Write("Section1", "Key1", "Value1");
            Mock.Write("Section1", "Key2", "Value2");
            Assert.AreEqual("[test.Section1]\r\nKey1=\"Value1\"\r\nKey2=\"Value2\"", Mock.GetSerialized());

            BMyIni MockB = new BMyIni("", "test");
            MockB.GetSerialized();
            MockB.Write("Section A", "Key 1", "value 1");
            MockB.Write("Section A", "Key 1", "value 2  ");
            MockB.Write("Section A", "Key 2", "valueG");
            MockB.Write("[foo]", "Bar", "baz");
            Assert.AreEqual("[test.Section A]\r\nKey 1=\"value 2  \"\r\nKey 2=\"valueG\"", MockB.GetSerialized());

            BMyIni MockC = new BMyIni(exampleINIStuff, "test");
            Assert.AreEqual(exampleINIStuff, MockC.GetSerialized());
            
        }

        [TestMethod()]
        public void readTest()
        {
            BMyIni Mock = new BMyIni(exampleINI, "test");
            Assert.IsNotNull(Mock.Read("owner", "name"));
            Assert.IsNotNull(Mock.Read("owner", "organization"));
            Assert.IsNotNull(Mock.Read("database", "server"));
            Assert.IsNotNull(Mock.Read("database", "port"));
            Assert.IsNotNull(Mock.Read("database", "file"));
            Assert.AreEqual("John Doe", Mock.Read("owner", "name"));
            Assert.AreEqual("Acme Widgets Inc.", Mock.Read("owner", "organization"));
            Assert.AreEqual("192.0.2.62", Mock.Read("database", "server"));
            Assert.AreEqual("143", Mock.Read("database", "port"));
            Assert.AreEqual("payroll.dat", Mock.Read("database", "file"));
            Assert.IsNull(Mock.Read("Foo", "Bar"));
            Assert.IsNull(Mock.Read("database", "name"));
            Assert.IsNull(Mock.Read("[owner]", "name"));
            Assert.IsNull(Mock.Read("[foo]", "port"));
            Assert.IsNull(Mock.Read("database", " file"));
        }

        [TestMethod()]
        public void writeTest()
        {
            BMyIni Mock = new BMyIni("[Section1]", "test");
            Assert.IsTrue(Mock.Write("Section1", "foo", "bar"));
            Assert.IsTrue(Mock.Write("Section2", "foo", "bar"));
            Assert.IsFalse(Mock.Write("[Section3]", "foo", "bar"));
        }

        [TestMethod()]
        public void removeTest()
        {
            BMyIni Mock = new BMyIni(exampleINI, "test");
            Assert.IsTrue(Mock.Remove("owner","name"));
            Assert.IsFalse(Mock.Remove("owner", "street"));
            Assert.IsFalse(Mock.Remove("owner", "street"));
            Assert.IsFalse(Mock.Remove("derp", "name"));

            Assert.IsTrue(Mock.Remove("owner"));
            Assert.IsNull(Mock.getSection("owner"));
        }
    }
}