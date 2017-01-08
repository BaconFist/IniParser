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
[owner]
name=John Doe
organization=Acme Widgets Inc.

[database]
; use IP address in case network name resolution is not working
server=192.0.2.62     
port=143
file=""payroll.dat""";

        [TestMethod()]
        public void BMyCustomDataTest()
        {
            BMyCustomData Mock = new BMyCustomData("");

            Assert.IsInstanceOfType(Mock, typeof(BMyCustomData));
        }

        [TestMethod()]
        public void getSerializedTest()
        {
            BMyCustomData Mock = new BMyCustomData("");
            Mock.addValue("Section1", "Key1", "Value1");
            Mock.addValue("Section1", "Key2", "Value2");
            Assert.AreEqual("[Section1]\r\nKey1=Value1\r\nKey2=Value2\r\n", Mock.getSerialized());

            BMyCustomData MockB = new BMyCustomData("");
            MockB.addValue("Section A", "Key 1", "value 1");
            MockB.addValue("Section A", "Key 1", "value 2  ");
            MockB.addValue("Section A", "Key 2", "valueG");
            MockB.addValue("[foo]", "Bar", "baz");
            Assert.AreEqual("[Section A]\r\nKey 1=\"value 2  \"\r\nKey 2=valueG\r\n", MockB.getSerialized());
        }

        [TestMethod()]
        public void CountSectionsTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI);
            Assert.AreEqual(2, Mock.CountSections());
            Mock.addSection("Derp");
            Assert.AreEqual(3, Mock.CountSections());
        }

        [TestMethod()]
        public void CountValuesTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI);
            Assert.AreEqual(2, Mock.CountValues("owner"));
            Assert.AreEqual(3, Mock.CountValues("database"));
            Assert.AreEqual(0, Mock.CountValues("[owner]"));
        }

        [TestMethod()]
        public void hasSectionTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI);
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
            BMyCustomData Mock = new BMyCustomData(exampleINI);
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
            BMyCustomData Mock = new BMyCustomData("[Section1]");
            Assert.IsTrue(Mock.addValue("Section1", "foo", "bar"));
            Assert.IsTrue(Mock.addValue("Section2", "foo", "bar"));
            Assert.IsFalse(Mock.addValue("[Section3]", "foo", "bar"));
        }

        [TestMethod()]
        public void addSectionTest()
        {
            BMyCustomData Mock = new BMyCustomData("");
            Assert.IsTrue(Mock.addSection("Derp"));
            Assert.IsFalse(Mock.addSection("[dulli]"));
        }

        [TestMethod()]
        public void getSectionTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI);
            Assert.IsNotNull(Mock.getSection("owner"));
            Assert.IsNotNull(Mock.getSection("database"));
            Assert.IsNull(Mock.getSection("[owner]"));
            Assert.IsNull(Mock.getSection("[database]"));
            Assert.IsNull(Mock.getSection("dulli"));
        }

        [TestMethod()]
        public void hasValueTest()
        {
            BMyCustomData Mock = new BMyCustomData(exampleINI);
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