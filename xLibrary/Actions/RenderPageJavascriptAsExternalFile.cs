namespace xLibrary.Actions
{
    using Chains;

    public sealed class RenderPageJavascriptAsExternalFile : IChainableAction<xTagContext, HttpResultContextWithxContext>
    {
        public HttpResultContextWithxContext Act(xTagContext context)
        {
            if (context.xTag.TagName != "body")
            {
                return
                    new RenderPageJavascriptAsExternalFile().Act(
                        new xTagContext(context.Parent, context.xTag.GetTagsByTagName("body")[0]));
            }

            var httpResultContext = new HttpResultContextWithxContext(context.Parent, contentType: "text/javascript");

            httpResultContext.ResponseText.AppendLine(
                "(function() { \"use strict\";\nfunction __onload() {window.xTags.SetJQ(window.jQuery);");

            for (var n = 0; n < context.Parent.JsText.Count; ++n)
            {
                httpResultContext.ResponseText.AppendLine(context.Parent.JsText[n] + ";");
            }

            httpResultContext.Aggregate(context, new RenderAsJavascriptClientModel());
            httpResultContext.ResponseText.AppendLine("};");
            httpResultContext.ResponseText.AppendLine("window.addEventListener('load',__onload);})();");

            return httpResultContext;
        }
    }
}
