namespace xLibrary.Actions
{
    using System.Text;

    using Chains;

    public class RenderPageJavascript : IChainableAction<xTagContext, HttpResultContextWithxContext>
    {
        public const string jQueryUri = "http://code.jquery.com/jquery-2.0.0.min.js";
        public const string xTagUri = "http://xtags.msd.am/xTags/xTag.min.js";
        private readonly bool withPrerequisites;

        public RenderPageJavascript(bool withPrerequisites = true)
        {
            this.withPrerequisites = withPrerequisites;
        }

        public HttpResultContextWithxContext Act(xTagContext context)
        {
            return this.RenderPageJs(context);
        }

        HttpResultContextWithxContext RenderPageJs(xTagContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context.Parent);

            if (withPrerequisites)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, string.Format("<script type='text/javascript' defer='defer' async='async' src='{0}'></script>", jQueryUri));
                renderItemToStringBuilder(httpResultContext.ResponseText, string.Format("<script type='text/javascript' defer='defer' async='async' src='{0}'></script>", xTagUri));
            }

            for (var n = 0; n < context.Parent.JsLinks.Count; ++n)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, "<script type='text/javascript' defer='defer' async='async' src='" + context.Parent.JsLinks[n] + "'></script>");
            }

            // Normal scripts must be loaded like 'defer', that is onload
            httpResultContext.ResponseText.AppendLine("<script type='text/javascript'><!--");
            httpResultContext.ResponseText.AppendLine("\"use strict\";");
            httpResultContext.ResponseText.AppendLine("(function() { function __onload() {window.xTags.SetJQ(window.jQuery);");
            for (var n = 0; n < context.Parent.JsText.Count; ++n)
            {
                httpResultContext.ResponseText.AppendLine(context.Parent.JsText[n] + ";");
            }

            httpResultContext.Aggregate(context, new RenderAsJavascriptClientModel());
            httpResultContext.ResponseText.AppendLine("};");
            httpResultContext.ResponseText.AppendLine("window.addEventListener('load',__onload);})();"); // DOMContentLoaded - async problem with xtag
            httpResultContext.ResponseText.Append("--></script>");

            return httpResultContext;
        }

        void renderItemToStringBuilder(StringBuilder sb, string line)
        {
            sb.Append(line);
        }
    }
}
