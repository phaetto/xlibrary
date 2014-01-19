namespace xLibrary.Actions
{
    using Chains;

    public class RenderJsonpIfRequested : IChainableAction<HttpResultContextWithxContext,HttpResultContextWithxContext>
    {
        public HttpResultContextWithxContext Act(HttpResultContextWithxContext context)
        {
            if (!string.IsNullOrEmpty(context.xContext.ContextInfo["callback"]))
            {
                var responseText = context.ResponseText.ToString();
                context.ResponseText.Remove(0, context.ResponseText.Length);
                context.ResponseText.Append(responseJsonpResponse(responseText, context.xContext));
                context.ContentType = "text/javascript";
            }

            return context;
        }

        string responseJsonpResponse(string responseText, xContext context)
        {
            return context.ContextInfo.QueryString["callback"] + "(\"" + JavascriptEscape(responseText) + "\");";
        }

        public static string JavascriptEscape(string str)
        {
            return str != null ? str.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "").Replace("\"", "\\\"") : "";
        }
    }
}
