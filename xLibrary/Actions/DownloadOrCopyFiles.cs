namespace xLibrary.Actions
{
    using System;
    using System.IO;
    using System.Net;
    using System.Web;
    using Chains;

    public sealed class DownloadOrCopyFiles : IChainableAction<xContext, xContext>
    {
        private readonly string[] uris;

        public DownloadOrCopyFiles(string[] uris)
        {
            this.uris = uris;
        }

        public xContext Act(xContext context)
        {
            DownloadOrCopyFile(context, RenderPageJavascript.jQueryUri);
            DownloadOrCopyFile(context, RenderPageJavascript.xTagUri);

            for (int n = 0; n < context.JsLinks.Count; ++n)
            {
                DownloadOrCopyFile(context, context.JsLinks[n]);
            }

            for (int n = 0; n < context.CssLinks.Count; ++n)
            {
                DownloadOrCopyFile(context, context.CssLinks[n]);
            }

            foreach (var uri in uris)
            {
                DownloadOrCopyFile(context, uri);
            }

            return context;
        }

        public static void DownloadOrCopyFile(xContext context, string uri)
        {
            if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                var uriObject = new Uri(uri);
                var fileName = VirtualPathUtility.GetFileName(uriObject.AbsolutePath);
                var directory = "~" + VirtualPathUtility.GetDirectory(uriObject.AbsolutePath);
                if (!string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(directory))
                {
                    directory = context.ContextInfo.ServerMapPath(directory);

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var client = new WebClient())
                    {
                        client.DownloadFile(uri, directory + fileName);
                    }
                }
            }
            else
            {
                var fileName = VirtualPathUtility.GetFileName(uri);
                var directory = VirtualPathUtility.GetFileName(uri);
                directory = context.ContextInfo.ServerMapPath(directory);
                File.Copy(uri, directory + fileName, true);
            }
        }

        public static string LocalUriForWebFile(string uri)
        {
            if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                var uriObject = new Uri(uri);
                var fileName = VirtualPathUtility.GetFileName(uriObject.AbsolutePath);
                var directory = VirtualPathUtility.GetDirectory(uriObject.AbsolutePath);

                if (!string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(directory))
                    return directory + fileName;
            }

            return null;
        }

        public static string RetrieveTextResource(string uri, xContext context)
        {
            var path = context.FixPathToFileOrHttpPath(uri);

            if (context.IsExternalUri(path))
            {
                var wr = WebRequest.Create(path);
                var resp = wr.GetResponse();
                var sr = new StreamReader(resp.GetResponseStream());

                return sr.ReadToEnd().Trim();
            }
            else
            {
                return File.ReadAllText(path);
            }
        }
    }
}
