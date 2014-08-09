namespace xLibrary.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Web;

    using Chains;

    sealed public class CheckIfRestRequest : IChainableAction<xTagContext, HttpResultContextWithxContext>
    {
        public const string NoToken = "0xdead";

        readonly private Random random = new Random();
        public readonly bool renewCsrfTokensForEachRequest;
        public readonly bool useCsrfCookies;
        public readonly bool csrfProtectionEnabled;
        public readonly bool checkAlsoChildrenForREST;
        public readonly bool skipCheckForId;
        public readonly Action<xTagContext, bool> onGet;
        public readonly Action<xTagContext, bool> onPost;
        public readonly Action<xTagContext, bool> onPut;
        public readonly Action<xTagContext, bool> onDelete;
        public readonly Action<xTagContext, bool, string> onCustomVerb;
        public readonly Action<xTagContext, bool, string> onAjax;

        public CheckIfRestRequest(
            Action<xTagContext, bool> onGet = null,
            Action<xTagContext, bool> onPost = null,
            Action<xTagContext, bool> onPut = null,
            Action<xTagContext, bool> onDelete = null,
            Action<xTagContext, bool, string> onCustomVerb = null,
            Action<xTagContext, bool, string> onAjax = null,
            bool csrfProtectionEnabled = true,
            bool renewCsrfTokensForEachRequest = true,
            bool useCsrfCookies = false,
            bool checkAlsoChildrenForREST = false,
            bool skipCheckForId = false)
        {
            this.renewCsrfTokensForEachRequest = renewCsrfTokensForEachRequest;
            this.useCsrfCookies = useCsrfCookies;
            this.csrfProtectionEnabled = csrfProtectionEnabled;
            this.onGet = onGet;
            this.onPost = onPost;
            this.onPut = onPut;
            this.onDelete = onDelete;
            this.onCustomVerb = onCustomVerb;
            this.onAjax = onAjax;
            this.checkAlsoChildrenForREST = checkAlsoChildrenForREST;
            this.skipCheckForId = skipCheckForId;
        }

        public HttpResultContextWithxContext Act(xTagContext context)
        {
            return this.CheckREST(context.Parent, context.xTag);
        }

        private HttpResultContextWithxContext CheckREST(xContext context, xTag xtag)
        {
            if (xtag.Mode == xTagMode.Server)
            {
                var httpResultContext = new HttpResultContextWithxContext(context);
                EnsureCSRFTokenExists(httpResultContext, xtag);

                var method = context.ContextInfo.HttpMethod.ToUpperInvariant();

                var argumentsCollection = method == "GET" ? context.ContextInfo.QueryString : context.ContextInfo.Form;

                if (method == "GET" && !string.IsNullOrEmpty(argumentsCollection["xtags-http-method"]))
                {
                    method = argumentsCollection["xtags-http-method"].ToUpperInvariant();
                }

                bool isSelfCalling = false;
                bool isAjax = argumentsCollection["xtags-xajax"] != null;
                if (skipCheckForId || argumentsCollection["xtags-id"] == xtag.GetId())
                {
                    // Only values, otherwise it hits the normal GET
                    if (method == "GET" && onGet == null && onAjax == null
                        && argumentsCollection["xtags-values-only"] != "xtags-values-only")
                    {
                        throw new NotSupportedException();
                    }

                    if (method == "POST" && onPost == null && onAjax == null)
                    {
                        throw new NotSupportedException();
                    }

                    if (method == "PUT" && onPut == null && onAjax == null)
                    {
                        throw new NotSupportedException();
                    }

                    if (method == "DELETE" && onDelete == null && onAjax == null)
                    {
                        throw new NotSupportedException();
                    }

                    isSelfCalling = true;
                }

                var isTokenMismatch = false;
                if (csrfProtectionEnabled && (isSelfCalling || method != "GET"))
                {
                    // If has been sent from the request and match
                    if (!IsCSRFTokenValid(context, xtag, argumentsCollection))
                    {
                        // CSRF protection - re-send it
                        isTokenMismatch = true;
                    }

                    if (renewCsrfTokensForEachRequest || isTokenMismatch)
                    {
                        // When xtag token has been used, create another one
                        CreateCSRFToken(httpResultContext, xtag);
                    }
                }

                if (!string.IsNullOrEmpty(argumentsCollection["xtags-session"]))
                {
                    xtag.Session = Uri.UnescapeDataString(argumentsCollection["xtags-session"]);
                }

                if (!string.IsNullOrEmpty(argumentsCollection["xtags-apikey"]))
                {
                    xtag.ApiKey = Uri.UnescapeDataString(argumentsCollection["xtags-apikey"]);
                }

                // Apply the values to the server tag
                if (isSelfCalling && !isTokenMismatch)
                {
                    var newHt = new Dictionary<string, string>();
                    foreach (var key in xtag.Data.Keys)
                    {
                        if (!string.IsNullOrEmpty(argumentsCollection[key]) && key != "callback"
                            && !key.StartsWith("xtags-"))
                        {
                            newHt[key] = Uri.UnescapeDataString(argumentsCollection[key]);
                            if (xtag.Attributes.ContainsKey(key))
                            {
                                xtag.Attributes[key] = newHt[key];
                            }
                        }
                        else
                        {
                            newHt[key] = xtag.Data[key];
                        }
                    }

                    xtag.Data = newHt;
                }

                if (isSelfCalling && !isTokenMismatch)
                {
                    if (method == "GET" && onGet != null)
                        onGet(new xTagContext(context, xtag), isAjax);
                    if (method == "POST" && onPost != null)
                        onPost(new xTagContext(context, xtag), isAjax);
                    if (method == "PUT" && onPut != null)
                        onPut(new xTagContext(context, xtag), isAjax);
                    if (method == "DELETE" && onDelete != null)
                        onDelete(new xTagContext(context, xtag), isAjax);

                    if (method != "GET" && method != "POST" && method != "PUT" && method != "DELETE" && onCustomVerb != null)
                        onCustomVerb(new xTagContext(context, xtag), isAjax, method);
                }

                // Form posting xtags-return-url
                if (!isAjax && !string.IsNullOrEmpty(argumentsCollection["xtags-return-url"]))
                {
                    return new HttpResultContextWithxContext(context, argumentsCollection["xtags-return-url"], false);
                }

                if (isSelfCalling && isAjax)
                {
                    if (!isTokenMismatch && onAjax != null)
                        onAjax(new xTagContext(context, xtag), isAjax, method);

                    if (argumentsCollection["xtags-token"] == "xtags-token" || isTokenMismatch)
                    {
                        // Get only the CSRF token
                        httpResultContext.Aggregate(new xTagContext(context, xtag), new RenderJsonValues(renderOnlyToken: true));
                    }
                    else if (argumentsCollection["xtags-values-only"] == "xtags-values-only")
                    {
                        // Get only values, rendered as JSON
                        httpResultContext.Aggregate(new xTagContext(context, xtag), new RenderJsonValues());
                    }
                    else if (argumentsCollection["xtags-html-get"] == "xtags-html-get")
                    {
                        // Html get for the parts that are supported from xtag service
                        httpResultContext.Aggregate(new xTagContext(context, xtag), new RenderPageBodyCssJsHtml());
                    }
                    else if (argumentsCollection["xtags-jsonp-repost"] == "xtags-jsonp-repost")
                    {
                        // Send a JSONP request to the specified xTags server origin
                        // TODO

                        // Get a token and a cookie if is xTags server (it will return JSON)
                        // Then send the request again
                    }
                    else if (argumentsCollection["xtags-no-response"] == "xtags-no-response")
                    {
                        // TODO: Should xtag be default for delete actions?
                        // No response on xtag resquest
                        return new HttpResultContextWithxContext(context, "No Content", 302);
                    }
                    else
                    {
                        // Render full xTags structure on javascript
                        httpResultContext.Aggregate(new xTagContext(context, xtag), new RenderAsJavascriptClientModel(renderElementsForAjax: false));
                    }

                    if (isSelfCalling || method != "GET")
                    {
                        // Use no cache when responding on postback requests
                        httpResultContext.NoCache();
                    }

                    httpResultContext.CompressRequest();
                    httpResultContext.AccessControlAllowOriginAll();
                    httpResultContext.ContentType = "text/plain";

                    return httpResultContext;
                }
            }

            if (checkAlsoChildrenForREST)
            {
                for (int i = 0; i < xtag.Children.Count; ++i)
                {
                    var result = CheckREST(context, xtag.Children[i]);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }


        public void EnsureCSRFTokenExists(HttpResultContextWithxContext httpResultContext, xTag xtag)
        {
            // REST CSRF protection
            if (string.IsNullOrEmpty(xtag.Token))
            {
                if (!csrfProtectionEnabled)
                {
                    xtag.Token = NoToken;
                    return;
                }

                var name = GetCSRFNameTag(xtag);
                if (!useCsrfCookies && httpResultContext.xContext.ContextInfo.HasSession())
                {
                    if (httpResultContext.xContext.ContextInfo.Session(name) != null)
                        xtag.Token = httpResultContext.xContext.ContextInfo.Session(name).ToString();
                    else
                        CreateCSRFToken(httpResultContext, xtag);
                }
                else
                {
                    if (httpResultContext.xContext.ContextInfo.Cookies[name] != null)
                        xtag.Token = httpResultContext.xContext.ContextInfo.Cookies[name].Value;
                    else
                        CreateCSRFToken(httpResultContext, xtag);
                }
            }
        }

        public bool IsCSRFTokenValid(xContext context, xTag xtag, NameValueCollection argumentsCollection)
        {
            if (string.IsNullOrEmpty(argumentsCollection["xtags-token"]))
            {
                return false;
            }

            var name = GetCSRFNameTag(xtag);
            if (!useCsrfCookies && context.ContextInfo.HasSession())
            {
                var objectInSession = context.ContextInfo.Session(name);
                return objectInSession != null && objectInSession.ToString() == Uri.UnescapeDataString(argumentsCollection["xtags-token"]);
            }

            return context.ContextInfo.Cookies[name] != null && context.ContextInfo.Cookies[name].Value == Uri.UnescapeDataString(argumentsCollection["xtags-token"]);
        }

        public void CreateCSRFToken(HttpResultContextWithxContext httpResultContext, xTag xtag)
        {
            // REST CSRF protection
            var randomBytes = new byte[32];
            random.NextBytes(randomBytes);
            var name = GetCSRFNameTag(xtag);

            xtag.Token = Convert.ToBase64String(randomBytes);

            if (!useCsrfCookies && httpResultContext.xContext.ContextInfo.HasSession())
            {
                httpResultContext.xContext.ContextInfo.Session(name, xtag.Token);
            }
            else
            {
                var cookie = httpResultContext.ResponseCookies[name];

                if (cookie == null)
                {
                    cookie = new HttpCookie(name);
                }

                cookie.HttpOnly = true;
                cookie.Value = Convert.ToBase64String(randomBytes);
                cookie.Domain = httpResultContext.xContext.ContextInfo.Host;
                httpResultContext.ResponseCookies.Add(cookie);
            }
        }

        public string GetCSRFNameTag(xTag xtag)
        {
            return xtag.GetId();
        }
    }
}
