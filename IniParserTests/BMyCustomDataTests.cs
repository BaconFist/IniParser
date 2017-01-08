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
    public class BMyCustomDataTests
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

        [TestMethod()]
        public void BMyCustomDataTest()
        {
            BMyCustomData Mock = new BMyCustomData("", "test");

            Assert.IsInstanceOfType(Mock, typeof(BMyCustomData));

            BMyCustomData MockC = new BMyCustomData(exampleIniMulitline, "test");
            Assert.IsTrue(MockC.hasSection("Codes"));
            Assert.IsTrue(MockC.hasValue("Codes", "script 1"));
            Assert.IsTrue(MockC.hasValue("Codes", "script 2"));
            Assert.IsFalse(MockC.hasValue("Codes", ""));
            Assert.AreEqual("class Foo {\n  public Foo(){\n    // do stuff\n  }\n}", MockC.getValue("Codes", "script 1"));
            Assert.AreEqual("    stuff in\nmultiple lines", MockC.getValue("Codes", "script 2"));
        }

        [TestMethod()]
        public void getSerializedTest()
        {
            BMyCustomData Mock = new BMyCustomData("", "test");
            Mock.addValue("Section1", "Key1", "Value1");
            Mock.addValue("Section1", "Key2", "Value2");
            Assert.AreEqual("[test.Section1]\r\nKey1=Value1\r\nKey2=Value2\r\n", Mock.getSerialized());

            BMyCustomData MockB = new BMyCustomData("", "test");
            MockB.addValue("Section A", "Key 1", "value 1");
            MockB.addValue("Section A", "Key 1", "value 2  ");
            MockB.addValue("Section A", "Key 2", "valueG");
            MockB.addValue("[foo]", "Bar", "baz");
            Assert.AreEqual("[test.Section A]\r\nKey 1=\"value 2  \"\r\nKey 2=valueG\r\n", MockB.getSerialized());


        }

        [TestMethod()]
        public void CountSectionsTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI, "test");
            Assert.AreEqual(2, Mock.CountSections());
            Mock.addSection("Derp");
            Assert.AreEqual(3, Mock.CountSections());
        }

        [TestMethod()]
        public void CountValuesTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI, "test");
            Assert.AreEqual(2, Mock.CountValues("owner"));
            Assert.AreEqual(3, Mock.CountValues("database"));
            Assert.AreEqual(0, Mock.CountValues("[owner]"));
        }

        [TestMethod()]
        public void hasSectionTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI, "test");
            Assert.IsTrue(Mock.hasSection("owner"));
            Assert.IsTrue(Mock.hasSection("database"));
            Assert.IsFalse(Mock.hasSection("[owner]"));
            Assert.IsFalse(Mock.hasSection("Database"));
            Assert.IsFalse(Mock.hasSection("foo"));
            Assert.IsFalse(Mock.hasSection("bar"));
        }

        [TestMethod()]
        public void getValueTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI, "test");
            Assert.IsNotNull(Mock.getValue("owner", "name"));
            Assert.IsNotNull(Mock.getValue("owner", "organization"));
            Assert.IsNotNull(Mock.getValue("database", "server"));
            Assert.IsNotNull(Mock.getValue("database", "port"));
            Assert.IsNotNull(Mock.getValue("database", "file"));
            Assert.AreEqual("John Doe", Mock.getValue("owner", "name"));
            Assert.AreEqual("Acme Widgets Inc.", Mock.getValue("owner", "organization"));
            Assert.AreEqual("192.0.2.62", Mock.getValue("database", "server"));
            Assert.AreEqual("143", Mock.getValue("database", "port"));
            Assert.AreEqual("payroll.dat", Mock.getValue("database", "file"));
            Assert.IsNull(Mock.getValue("Foo", "Bar"));
            Assert.IsNull(Mock.getValue("database", "name"));
            Assert.IsNull(Mock.getValue("[owner]", "name"));
            Assert.IsNull(Mock.getValue("[foo]", "port"));
            Assert.IsNull(Mock.getValue("database", " file"));
        }

        [TestMethod()]
        public void addValueTest()
        {
            BMyCustomData Mock = new BMyCustomData("[Section1]", "test");
            Assert.IsTrue(Mock.addValue("Section1", "foo", "bar"));
            Assert.IsTrue(Mock.addValue("Section2", "foo", "bar"));
            Assert.IsFalse(Mock.addValue("[Section3]", "foo", "bar"));
        }

        [TestMethod()]
        public void addSectionTest()
        {
            BMyCustomData Mock = new BMyCustomData("", "test");
            Assert.IsTrue(Mock.addSection("Derp"));
            Assert.IsFalse(Mock.addSection("[dulli]"));
        }

        [TestMethod()]
        public void getSectionTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI, "test");
            Assert.IsNotNull(Mock.getSection("owner"));
            Assert.IsNotNull(Mock.getSection("database"));
            Assert.IsNull(Mock.getSection("[owner]"));
            Assert.IsNull(Mock.getSection("[database]"));
            Assert.IsNull(Mock.getSection("dulli"));
        }

        [TestMethod()]
        public void hasValueTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI, "test");
            Assert.IsTrue(Mock.hasValue("owner", "name"));
            Assert.IsTrue(Mock.hasValue("owner", "organization"));
            Assert.IsTrue(Mock.hasValue("database", "server"));
            Assert.IsTrue(Mock.hasValue("database", "port"));
            Assert.IsTrue(Mock.hasValue("database", "file"));
            Assert.IsFalse(Mock.hasValue("foo", "bar"));
            Assert.IsFalse(Mock.hasValue("Database", "file"));
            Assert.IsFalse(Mock.hasValue("owner ", "name"));
        }
    }
}