namespace xLibrary.Actions
{
    using Chains;

    public sealed class RenderPageJavascriptAsLink : IChainableAction<xTagContext, HttpResultContextWithxContext>
    {
        private readonly string link;
        private readonly bool withPrerequisites;

        public RenderPageJavascriptAsLink(string link, bool withPrerequisites = true)
        {
            this.link = link;
            this.withPrerequisites = withPrerequisites;
        }

        public HttpResultContextWithxContext Act(xTagContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context.Parent);

            if (withPrerequisites)
            {
                httpResultContext.ResponseText.AppendLine(
                    string.Format(
                        "<script type='text/javascript' defer='defer' async='async' src='{0}'></script>",
                        RenderPageJavascript.jQueryUri));
                httpResultContext.ResponseText.AppendLine(
                    string.Format(
                        "<script type='text/javascript' defer='defer' async='async' src='{0}'></script>",
                        RenderPageJavascript.xTagUri));
            }

            for (int n = 0; n < context.Parent.JsLinks.Count; ++n)
            {
                httpResultContext.ResponseText.AppendLine(
                    "<script type='text/javascript' defer='defer' async='async' src='"
                        + context.Parent.JsLinks[n] + "'></script>");
            }

            httpResultContext.ResponseText.AppendLine(
                "<script type='text/javascript' defer='defer' async='async' src='" + link + "'></script>");

            return httpResultContext;
        }
    }
}
