namespace xLibrary.Actions
{
    using System.Xml;
    using Chains;

    public class LoadLibrary : IChainableAction<xContext, xContext>
    {
        private readonly string uri;
        private readonly bool markAsSafe;

        private readonly XmlDocument document;

        public LoadLibrary(string uri, bool markAsSafe = false)
        {
            this.uri = uri;
            this.markAsSafe = markAsSafe;
        }

        public LoadLibrary(XmlDocument document, bool markAsSafe = false)
        {
            this.document = document;
            this.markAsSafe = markAsSafe;
        }

        public xContext Act(xContext context)
        {
            if (document != null)
                context.LoadLibrary(document);
            else
                context.LoadLibrary(uri);

            if (markAsSafe && !string.IsNullOrEmpty(uri))
                context.MarkUriAsSafe(uri);

            return context;
        }
    }
}
