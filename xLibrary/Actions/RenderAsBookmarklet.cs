namespace xLibrary.Actions
{
    using Chains;

    public class RenderAsBookmarklet: IChainableAction<xContext, HttpResultContextWithxContext>
    {
        private const string BookmarkletTemplate = @"(function () {
    function onXTagsReady() {
      /*Put your xTags related code here*/
      var tagName = ""{0}"";
      var libraryUri = ""{1}"";
      var handler = ""{2}"";
      var parentTag = ""{3}"";
    
      xTags.Import(tagName, libraryUri, handler);
      var xtag = xTags.CreateInstance(tagName);
      xtag.Create(parentTag);
    };
	 
    function loadScript(url, callback) {
        var script = document.createElement(""script"");
        script.type = ""text/javascript"";
        if (script.readyState) {
            script.onreadystatechange = function () {
                if (script.readyState == ""loaded"" || script.readyState == ""complete"") {
                    script.onreadystatechange = null;
                    callback();
                };
            };
        } else {
            script.onload = function () {
                callback();
            };
        };
        script.src = url;
        document.getElementsByTagName(""head"")[0].appendChild(script);
    };
	
	var xtagUri = """ + RenderPageJavascript.xTagUri + @""";
	if (typeof(jQuery) === ""undefined"") {
	    loadScript(""" + RenderPageJavascript.jQueryUri + @""", function () {
			  loadScript(xtagUri, function () {
				  onXTagsReady();
			});
		});
	}
	else if (typeof(xTags) === ""undefined"") {
		loadScript(xtagUri, function () {
			  onXTagsReady();
		});
	}
	else onXTagsReady();
})();";

        private readonly string tagName;
        private readonly string libraryUri;
        private readonly string handlerUri;
        private readonly string parentTag;

        public RenderAsBookmarklet(string tagName, string libraryUri, string handlerUri, string parentTag = "body")
        {
            this.tagName = tagName;
            this.libraryUri = libraryUri;
            this.handlerUri = handlerUri;
            this.parentTag = parentTag;
        }

        public HttpResultContextWithxContext Act(xContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context)
                                    {
                                        ContentType = "text/javascript"
                                    };

            httpResultContext.ResponseText.Append(
                string.Format(BookmarkletTemplate, tagName, libraryUri, handlerUri, parentTag));
            return httpResultContext;
        }
    }
}
