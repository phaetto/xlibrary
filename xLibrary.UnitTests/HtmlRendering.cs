namespace xLibrary.UnitTests
{
    using System.Xml;
    using Chains.Play.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using xLibrary;
    using xLibrary.Actions;

    [TestClass]
    public class HtmlRendering
    {
        [TestMethod]
        public void RenderHtml_WhenOneTagTemplate_ThenDivIsRenderedAsHtml()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' /></r>");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Do(new RenderHtml());

            // Structural elements
            Assert.AreEqual(result.ResponseText.ToString(), "<div id='a'></div>");
        }

        [TestMethod]
        public void RenderHtml_WhenLcidIsSetOnHtmlTag_ThenLanguageIsRenderedOnHtml()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template tag='html' /></r>");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template", lcid: 1033))
                              .Do(new RenderHtml());

            // Structural elements
            Assert.IsTrue(result.ResponseText.ToString().Contains("<html lang='en'"));
        }

        [TestMethod]
        public void RenderPageBodyCss_WhenOneTagTemplate_ThenNoStyleTagIsRendered()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' /></r>");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Parent
                              .Do(new RenderPageBodyCss());

            // No garbage is rendered when having no styles
            Assert.IsTrue(string.IsNullOrEmpty(result.ResponseText.ToString()));
        }

        [TestMethod]
        public void RenderPageHeadCss_WhenOneTagTemplate_ThenClickjackingButNoStyleTagIsRendered()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' /></r>");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Parent
                              .Do(new RenderPageHeadCss());

            var responseText = result.ResponseText.ToString();
            // Clickjacking
            Assert.IsNotNull(result.ResponseHeaders["X-Frame-Options"]);
            Assert.IsTrue(responseText.Contains("top.location.replace(self.location.href);"));
            // Does not contain style
            Assert.IsFalse(responseText.Contains("<style"));
        }

        [TestMethod]
        public void RenderPageBodyCssJsHtml_WhenOneTagTemplate_ThenDivIsRenderedInHtmlCssJavascript()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' /></r>");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Do(new RenderPageBodyCssJsHtml());

            var responseText = result.ResponseText.ToString();
            // Structural elements
            Assert.IsTrue(responseText.Contains("<div id='a'></div>"));
            // Javascript Libraries
            Assert.IsTrue(responseText.Contains(RenderPageJavascript.jQueryUri));
            Assert.IsTrue(responseText.Contains(RenderPageJavascript.xTagUri));
            // No style
            Assert.IsFalse(responseText.Contains("<style"));
        }

        [TestMethod]
        public void RenderHtml_WhenHtmlTemplate_ThenHasHtmlTextOnOutput()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template tag='html'><head /><body /></template></r>");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .Do(new RenderHtml());

            var responseText = result.ResponseText.ToString();
            // Structural elements
            Assert.IsTrue(responseText.Contains("<html><head>"));
            Assert.IsTrue(responseText.Contains("</head><body id='__body'>"));
            Assert.IsTrue(responseText.Contains("</body>"));
            // Clickjacking
            Assert.IsNotNull(result.ResponseHeaders["X-Frame-Options"]);
            Assert.IsTrue(responseText.Contains("top.location.replace(self.location.href);"));
            // Javascript Libraries
            Assert.IsTrue(responseText.Contains(RenderPageJavascript.jQueryUri));
            Assert.IsTrue(responseText.Contains(RenderPageJavascript.xTagUri));
            // No style
            Assert.IsFalse(responseText.Contains("<style"));
        }

        [TestMethod]
        public void RenderHtml_WhenNoHttpContext_ThenCheckIfRestRequestReturnsNull()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' /></r>");

            var result =
                new xContext().Do(new LoadLibrary(doc))
                              .Do(new CreateTag("template"))
                              .DoFirst(x => x != null, new CheckIfRestRequest(), new RenderHtml());

            var responseText = result.ResponseText.ToString();
            Assert.AreEqual(responseText, "<div id='a'></div>");
        }

        [TestMethod]
        public void RenderHtml_WhenUriWithTilde_ThenCheckIfLoadsTheFile()
        {
            const string xmlTemplate =
                "<?xml version=\"1.0\"?><r><template id=\"a\"><data type=\"text/javascript-events\" src=\"~/file.xml\" /></template></r>";

            var doc = new XmlDocument();
            doc.LoadXml(xmlTemplate);
            using (var writer = new XmlTextWriter("file.xml", null))
            {
                writer.Formatting = Formatting.None;
                doc.Save(writer);
            }

            var result =
                new xContext(new HttpContextInfo()).Do(new LoadLibrary(doc))
                                                   .Do(new CreateTag("template"));

            var responseText = result.Do(new RenderHtml()).ResponseText.ToString();
            Assert.AreEqual(responseText, "<div id='a'></div>");
            Assert.AreEqual(result.xTag.EventsData, xmlTemplate);
        }
    }
}
