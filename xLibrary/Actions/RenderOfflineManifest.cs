namespace xLibrary.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Xml;

    using Chains;

    public class RenderOfflineManifest : IChainableAction<xContext, HttpResultContextWithxContext>
    {
        private readonly string[] libraries;
        private readonly string[] extraResources;
        private readonly string fallbackPage;

        public RenderOfflineManifest(string[] libraries, string[] extraResources = null, string fallbackPage = null)
        {
            this.libraries = libraries;
            this.extraResources = extraResources ?? new string[0];
            this.fallbackPage = fallbackPage;
        }

        public HttpResultContextWithxContext Act(xContext context)
        {
            return RenderOfflineManifestNow(context);
        }

        private HttpResultContextWithxContext RenderOfflineManifestNow(xContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context, contentType: "text/cache-manifest");

            // Gather all resources, try to check if they have been changed
            string lastTimeString = "";
            List<string> urisToCache = new List<string>();
            //List<string> urisForNetwork = new List<string>();

            // Put the prerequisites
            urisToCache.Add(RenderPageJavascript.jQueryUri);
            urisToCache.Add(RenderPageJavascript.xTagUri);

            foreach (string library in libraries)
            {
                string libraryPath = context.FixPathToFileOrHttpPath(library);
                string libraryWebPath = context.FixPathToWebPath(library);
                if (urisToCache.IndexOf(libraryWebPath) == -1)
                    urisToCache.Add(libraryWebPath);

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(libraryPath);
                var images = xmlDoc.SelectNodes("//*/*/img[@src]");
                var css = xmlDoc.SelectNodes("//*/*/data[@type='text/css' and @src]");
                var js = xmlDoc.SelectNodes("//*/*/data[@type='text/javascript' and @src]");
                var phraseLibraries = xmlDoc.SelectNodes("//*/*/phrase[@src]");

                // Put all the URIs in the list
                for (var n = 0; n < images.Count; ++n)
                {
                    if (context.IsOkToRender(images[n]))
                    {
                        string uri = context.FixPathToWebPath(images[n].Attributes["src"].Value);
                        if (urisToCache.IndexOf(uri) > -1) continue;
                        urisToCache.Add(uri);
                    }
                }
                for (int n = 0; n < css.Count; ++n)
                {
                    if (context.IsOkToRender(css[n]))
                    {
                        string uri = context.FixPathToWebPath(css[n].Attributes["src"].Value);
                        if (urisToCache.IndexOf(uri) > -1) continue;
                        urisToCache.Add(uri);
                    }
                }
                for (int n = 0; n < js.Count; ++n)
                {
                    if (context.IsOkToRender(js[n]))
                    {
                        string uri = context.FixPathToWebPath(js[n].Attributes["src"].Value);
                        if (urisToCache.IndexOf(uri) > -1) continue;
                        urisToCache.Add(uri);
                    }
                }
                for (int n = 0; n < phraseLibraries.Count; ++n)
                {
                    string uri = context.FixPathToWebPath(phraseLibraries[n].Attributes["src"].Value);
                    if (urisToCache.IndexOf(uri) > -1) continue;
                    urisToCache.Add(uri);
                }

                //XmlNodeList serverOrigins = xmlDoc.SelectNodes("//*/*[@ServerOrigin]");
                //for (int n = 0; n < serverOrigins.Count; ++n)
                //{
                //    string uri = FixPathToWebPath(serverOrigins[n].Attributes["ServerOrigin"].Value);
                //    if (urisForNetwork.IndexOf(uri) > -1) continue;
                //    urisForNetwork.Add(uri);
                //}

                if (!context.IsExternalUri(libraryPath))
                    lastTimeString += (string.IsNullOrEmpty(lastTimeString) ? "" : ", ") + System.IO.File.GetLastWriteTimeUtc(libraryPath).ToString();
            }

            if (extraResources != null && extraResources.Length > 0)
            {
                for (int n = 0; n < extraResources.Length; ++n)
                {
                    string uri = context.FixPathToWebPath(extraResources[n]);
                    if (urisToCache.IndexOf(uri) > -1) continue;
                    urisToCache.Add(uri);
                }
            }

            httpResultContext.NoCache();
            httpResultContext.CompressRequest();

            // The timestamp on manifest
            httpResultContext.ResponseText.Append("CACHE MANIFEST\n");
            httpResultContext.ResponseText.Append("# Last-modified: " + lastTimeString + "\n\n");

            // Add services as not offline
            httpResultContext.ResponseText.Append("NETWORK:\n*\nhttp://*\nhttps://*\n");
            httpResultContext.ResponseText.Append(context.ContextInfo.RequestPath + "\n\n");
            httpResultContext.ResponseText.Append("CACHE:\n");
            for (int n = 0; n < urisToCache.Count; ++n)
            {
                httpResultContext.ResponseText.Append(urisToCache[n] + "\n");
            }

            // Fallback
            if (!string.IsNullOrEmpty(fallbackPage))
            {
                httpResultContext.ResponseText.Append("FALLBACK:\n");
                httpResultContext.ResponseText.Append("/ " + fallbackPage + "\n");
            }

            return httpResultContext;
        }
    }
}
