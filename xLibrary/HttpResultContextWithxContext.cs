namespace xLibrary
{
    using Chains.Play.Web;

    public sealed class HttpResultContextWithxContext : HttpResultContextBase<HttpResultContextWithxContext>
    {
        public readonly xContext xContext;

        public HttpResultContextWithxContext(
            xContext xContext, string resultText = "", string contentType = "text/html", int statusCode = 200)
            : base(resultText, contentType, statusCode)
        {
            this.xContext = xContext;
        }

        public HttpResultContextWithxContext(xContext xContext, string statusText, int statusCode)
            : base(statusText, statusCode)
        {
            this.xContext = xContext;
        }

        public HttpResultContextWithxContext(xContext xContext, string redirectTo, bool permanentRedirect)
            : base(redirectTo, permanentRedirect)
        {
            this.xContext = xContext;
        }

        public void ApplyOutputToHttpContext()
        {
            if (this.xContext.ContextInfo.HttpContext != null)
            {
                this.ApplyOutputToHttpContext(this.xContext.ContextInfo.HttpContext);
            }
            else
            {
                this.ApplyOutputToHttpContext(this.xContext.ContextInfo.HttpListenerContext);
            }
        }
    }
}
