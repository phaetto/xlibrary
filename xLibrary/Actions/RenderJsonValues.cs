namespace xLibrary.Actions
{
    using Chains;

    public class RenderJsonValues : IChainableAction<xTagContext, HttpResultContextWithxContext>
    {
        private readonly bool renderOnlyToken;

        public RenderJsonValues(bool renderOnlyToken = false)
        {
            this.renderOnlyToken = renderOnlyToken;
        }

        public HttpResultContextWithxContext Act(xTagContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context.Parent);

            httpResultContext.ResponseText.Append("{");

            if (renderOnlyToken)
            {
                if (!string.IsNullOrEmpty(context.xTag.Token))
                {
                    if (context.Parent.ContextInfo["xtags-token"] == "xtags-token")
                        httpResultContext.ResponseText.Append("\"xtags-new-token\":\"" + jsEscape(context.xTag.Token) + "\"");
                    else
                        httpResultContext.ResponseText.Append("\"xtags-renew-token\":\"" + jsEscape(context.xTag.Token) + "\"");
                }
            }
            else
            {
                httpResultContext.ResponseText.Append("\"xtags-session\":\"" + (!string.IsNullOrEmpty(context.xTag.Session) ? context.xTag.Session : string.Empty) + "\"");

                if (!string.IsNullOrEmpty(context.xTag.Token))
                    httpResultContext.ResponseText.Append(",\"xtags-token\":\"" + jsEscape(context.xTag.Token) + "\"");

                foreach (string key in context.xTag.Data.Keys)
                {
                    if (context.xTag.Data[key] != null)
                    {
                        httpResultContext.ResponseText.Append(
                           ",\"" + key + "\":\"" + jsEscape(context.xTag.Data[key] as string) + "\"");
                    }
                }
            }

            httpResultContext.ResponseText.Append("}");

            return httpResultContext;
        }

        string jsEscape(string str)
        {
            return str != null ? str.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "").Replace("\"", "\\\"") : "";
        }
    }
}
