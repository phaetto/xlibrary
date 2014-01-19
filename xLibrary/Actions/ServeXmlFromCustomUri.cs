namespace xLibrary.Actions
{
    using System;
    using System.Xml;

    using Chains;

    public class ServeXmlFromCustomUri : IChainableAction<xContext, HttpResultContextWithxContext>
    {
        public HttpResultContextWithxContext Act(xContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context, contentType: "text/javascript");
            // JSONP Callback to load xml resource from another server
            if (!string.IsNullOrEmpty(context.ContextInfo.QueryString["xtags-jsonp-xml"])
                && context.ContextInfo.HttpMethod == "GET")
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(
                    context.FixPathToFileOrHttpPath(
                        Uri.UnescapeDataString(context.ContextInfo["xtags-jsonp-xml"])));

                httpResultContext.ResponseText.Append(xmlDoc.InnerXml);
            }

            return httpResultContext;
        }
    }
}
