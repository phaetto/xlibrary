namespace xLibrary.UnitTests
{
    using System.Xml;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using xLibrary;
    using xLibrary.Actions;

    [TestClass]
    public class DatabindBehavior
    {
        [TestMethod]
        public void Databind_WhenBoundToArray_ThenNodesAreTheSameNumber()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a'><span /></template></r>");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Do(new Databind(new[] { "One", "Two", "Three" }));

            Assert.AreEqual(result.xTag.Children.Count, 3);
        }

        [TestMethod]
        public void Databind_WhenBoundToArrayOfObjectsAndDataboundToProperty_ThenNodesHaveTheText()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a'><span>#{this.data()}</span></template></r>");

            object boundObject = new[]
                                 {
                                     new { data = "One" },
                                     new { data = "Two" },
                                     new { data = "Three" }
                                 };

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Do(new Databind(boundObject));

            Assert.AreEqual(result.xTag.Children.Count, 3);
            Assert.AreEqual(result.xTag.Children[0].Children[0].Text, "One");
            Assert.AreEqual(result.xTag.Children[1].Children[0].Text, "Two");
            Assert.AreEqual(result.xTag.Children[2].Children[0].Text, "Three");
        }

        [TestMethod]
        public void Databind_WhenBoundToObjectAndDataboundToProperty_ThenNodesHaveTheText()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a'><span>#{this.data()}</span></template></r>");

            object boundObject = new
                                 {
                                     data = "One"
                                 };

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Do(new Databind(boundObject));

            Assert.AreEqual(1, result.xTag.Children.Count);
            Assert.AreEqual("One", result.xTag.Children[0].Children[0].Text);
        }

        [TestMethod]
        public void Databind_WhenBoundToObjectFromJsonAndDataboundToProperty_ThenNodesHaveTheText()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a'><span>#{this.data()}</span></template></r>");

            var boundObject = JsonConvert.DeserializeObject("{ data:'One' }");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Do(new Databind(boundObject));

            Assert.AreEqual(1, result.xTag.Children.Count);
            Assert.AreEqual("One", result.xTag.Children[0].Children[0].Text);
        }

        [TestMethod]
        public void Databind_WhenBoundToXml_ThenChildNodesHaveTheCorrectText()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' datasource='/r/a'><span>#{this.Text()}</span></template></r>");

            var dataDoc = new XmlDocument();
            dataDoc.LoadXml("<r> <a>1</a> <a>2</a> <a>3</a> </r>");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Do(new Databind(dataDoc));

            Assert.AreEqual(result.xTag.Children.Count, 3);
            Assert.AreEqual(result.xTag.Children[0].Children[0].Text, "1");
            Assert.AreEqual(result.xTag.Children[1].Children[0].Text, "2");
            Assert.AreEqual(result.xTag.Children[2].Children[0].Text, "3");
        }

        [TestMethod]
        public void Datasource_WhenBoundToArrayOfObjectsAndADatasourceIsIncluded_ThenNodeHaveTheText()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a'><span>Not bound</span><span datasource='BoundSource'><span>#{this.data()}</span></span></template></r>");

            object boundObject = new
                                 {
                                     BoundSource = new
                                                   {
                                                       data = "One"
                                                   }
                                 };

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Do(new Databind(boundObject));

            Assert.AreEqual(2, result.xTag.Children.Count);
            Assert.AreEqual(1, result.xTag.Children[1].Children.Count);
            Assert.AreEqual(1, result.xTag.Children[1].Children[0].Children.Count);
            Assert.AreEqual("One", result.xTag.Children[1].Children[0].Children[0].Text);
        }
    }
}
