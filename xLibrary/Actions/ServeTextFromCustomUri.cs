namespace xLibrary.Actions
{
    using System;
    using System.IO;
    using System.Net;

    using Chains;

    public class ServeTextFromCustomUri : IChainableAction<xContext, HttpResultContextWithxContext>
    {
        public HttpResultContextWithxContext Act(xContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context, contentType: "text/javascript");
            // JSONP Callback to load xml resource from another server
            if (!string.IsNullOrEmpty(context.ContextInfo.QueryString["xtags-jsonp-txt"])
                && context.ContextInfo.HttpMethod == "GET")
            {
                var wr = WebRequest.Create(context.FixPathToFileOrHttpPath(Uri.UnescapeDataString(context.ContextInfo["xtags-jsonp-txt"])));
                var resp = wr.GetResponse();
                var sr = new StreamReader(resp.GetResponseStream());

                httpResultContext.ResponseText.Append(sr.ReadToEnd().Trim());
            }

            return httpResultContext;
        }
    }
}
