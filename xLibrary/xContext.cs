namespace xLibrary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Web;
    using System.Xml;
    using Chains;
    using Chains.Play.Web;

    sealed public class xContext : ChainWithHistory<xContext>
    {
        public IHttpContextInfo ContextInfo { get; private set; }

        public xContext()
            : this(new HttpContextInfo(HttpContext.Current))
        {
        }

        public xContext(IHttpContextInfo contextInfo)
        {
            ContextInfo = contextInfo;

            LibrariesLoaded = new List<string>();
            Libraries = new List<XmlDocument>();
            Templates = new Hashtable();
            AllowedExternalServerOrigin = new List<string>();
            TemplateExternalServerOrigin = new List<string>();
            CssLinks = new List<string>();
            CssText = new List<string>();
            JsLinks = new List<string>();
            JsText = new List<string>();
        }

        #region Context-based variables

        public List<string> LibrariesLoaded { get; private set; }

        public List<XmlDocument> Libraries { get; private set; }

        public Hashtable Templates { get; private set; }

        public List<string> AllowedExternalServerOrigin { get; private set; }

        public List<string> TemplateExternalServerOrigin { get; private set; }

        public int AutoId { get; private set; }

        public List<string> CssLinks { get; private set; }

        public List<string> CssText { get; private set; }

        public List<string> JsLinks { get; private set; }

        public List<string> JsText { get; private set; }

        #endregion

        public xTag Create(string templateName, string libraryUri = null, string id = null)
        {
            if (!String.IsNullOrEmpty(libraryUri))
                LoadLibrary(libraryUri);

            if (!Templates.ContainsKey(templateName))
                throw new Exception("The template '" + templateName + "' does not exists.");

            XmlNode templateNode = Templates[templateName] as XmlNode;

            if (templateNode == null)
                throw new Exception(templateName + " template does not exists.");

            xTag xt = MakeTemplateNode(templateNode, null, null, null, false);

            if (!String.IsNullOrEmpty(id))
                xt.Id = id;

            return xt;
        }

        #region Libraries

        private string TryFixUriToAbsolute(string uri)
        {
            var virtualFolder = VirtualPathUtility.GetDirectory(ContextInfo.RequestPath);
            if (!String.IsNullOrEmpty(virtualFolder) && !uri.StartsWith("~") && !uri.StartsWith(virtualFolder)
                && !File.Exists(uri))
            {
                uri = VirtualPathUtility.Combine(virtualFolder, uri);
            }

            if (!uri.StartsWith("~") && !File.Exists(uri) && VirtualPathUtility.IsAbsolute(uri))
            {
                if (ContextInfo.HttpContext != null)
                {
                    uri = VirtualPathUtility.ToAppRelative(uri);
                }
            }

            if (!uri.StartsWith("~") && !File.Exists(uri) && VirtualPathUtility.IsAbsolute(uri))
            {
                if (ContextInfo.HttpContext != null)
                {
                    uri = "~" + uri;
                }
            }

            return uri;
        }

        public bool IsExternalUri(string libraryUri)
        {
            return libraryUri.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                || libraryUri.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)
                || libraryUri.StartsWith("//", StringComparison.InvariantCultureIgnoreCase);
        }

        public string FixPathToFileOrHttpPath(string libraryUri)
        {
            if (!IsExternalUri(libraryUri))
            {
                libraryUri = TryFixUriToAbsolute(libraryUri);

                if (libraryUri.StartsWith("~"))
                {
                    libraryUri = ContextInfo.ServerMapPath(libraryUri);
                }
            }

            return libraryUri;
        }

        public string FixPathToWebPath(string libraryUri)
        {
            if (!IsExternalUri(libraryUri))
            {
                libraryUri = TryFixUriToAbsolute(libraryUri);

                if (libraryUri.StartsWith("~"))
                {
                    try
                    {
                        libraryUri = VirtualPathUtility.ToAbsolute(libraryUri);
                    }
                    catch (HttpException)
                    {
                        libraryUri = VirtualPathUtility.ToAbsolute(libraryUri, "/");
                    }
                }
            }

            return libraryUri;
        }

        public void LoadLibrary(string libraryUri)
        {
            XmlDocument xmlDoc = new XmlDocument();

            // Caching libraries ability

            libraryUri = FixPathToFileOrHttpPath(libraryUri);

            if (LibrariesLoaded.IndexOf(libraryUri) > -1)
            {
                return;
            }

            xmlDoc.Load(libraryUri);
            this.LoadLibrary(xmlDoc, libraryUri);
        }

        public void MarkUriAsSafe(string uri)
        {
            if (AllowedExternalServerOrigin.IndexOf(uri.ToLowerInvariant()) == -1)
                AllowedExternalServerOrigin.Add(uri.ToLowerInvariant());
        }

        public bool IsUrlExternalServerOrigin(string uri)
        {
            if (IsExternalUri(uri) &&
                AllowedExternalServerOrigin.IndexOf(uri.ToLowerInvariant()) == -1 &&
                !uri.ToLowerInvariant().StartsWith(ContextInfo.ServerUri().ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase) &&
                !uri.ToLowerInvariant().StartsWith("http://xtags.msd.am/", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public void LoadLibrary(XmlDocument xmlDoc)
        {
            LoadLibrary(xmlDoc, "/");
        }

        public void LoadLibrarySafe(XmlDocument xmlDoc)
        {
            LoadLibrary(xmlDoc, null);
        }

        void LoadLibrary(XmlDocument xmlDoc, string libraryUri)
        {
            string ns = (xmlDoc.DocumentElement.Attributes["namespace"] != null ? xmlDoc.DocumentElement.Attributes["namespace"].Value + "." : "");
            XmlNodeList templateNodes = xmlDoc.SelectNodes("/*/*");
            for (int n = 0; n < templateNodes.Count; ++n)
            {
                if (templateNodes[n].Name != "data" && templateNodes[n].Name != "phrase" &&
                    templateNodes[n].Attributes["noTemplate"] == null)
                {
                    if (!Templates.ContainsKey(ns + templateNodes[n].Name) ||
                        !(
                            Templates.ContainsKey(ns + templateNodes[n].Name) &&
                            templateNodes[n].Attributes["src"] != null &&
                            (Templates[ns + templateNodes[n].Name] as XmlNode).Attributes["src"] == null
                            )
                        )
                    {
                        Templates[ns + templateNodes[n].Name] = templateNodes[n];

                        if (templateNodes[n].Attributes["version"] != null)
                        {
                            Templates[ns + templateNodes[n].Name + templateNodes[n].Attributes["version"].Value] = templateNodes[n];
                        }

                        if (String.IsNullOrEmpty(libraryUri) || IsUrlExternalServerOrigin(libraryUri))
                        {
                            TemplateExternalServerOrigin.Add(ns + templateNodes[n].Name);
                        }
                    }
                }
            }

            Libraries.Add(xmlDoc);
            if (!String.IsNullOrEmpty(libraryUri))
                LibrariesLoaded.Add(libraryUri);
        }

        XmlNode GetTemplateNode(XmlNode node)
        {
            if (node.Attributes["version"] != null)
                return GetTemplateNode(node.Name, node.Attributes["version"].Value);
            else
                return GetTemplateNode(node.Name, null);
        }

        XmlNode GetTemplateNode(string name, string version)
        {
            if (!String.IsNullOrEmpty(version) && Templates[name + version] != null)
                return Templates[name + version] as XmlNode;

            return Templates[name] as XmlNode;
        }

        #endregion

        #region Parsing / Analyzing

        public xTag MakeTemplateNode(XmlNode xmlNode, xTag mainTag, xTag parentTag, xTag rootTag, bool isTemplate, bool computeChildren = true)
        {
            if (xmlNode.NodeType == XmlNodeType.Element && !isTemplate && Templates[xmlNode.Name] != null) // Template
            {
                XmlNode templateNode = GetTemplateNode(xmlNode);
                if (templateNode.Attributes["src"] != null)
                {
                    xTag tempTag = MakeTemplateNode(templateNode, null, parentTag, rootTag, true, false);
                    if (IsUrlExternalServerOrigin(tempTag.Attributes["src"]))
                    {
                        // Get default template xDomainNotAllowed
                    }
                    else
                    {
                        LoadLibrary(tempTag.Attributes["src"]);
                        templateNode = GetTemplateNode(xmlNode);
                    }
                }

                xTag cTag = MakeTemplateNode(templateNode, null, parentTag, rootTag, true);
                cTag.MainTag = mainTag;

                if (mainTag != null)
                {
                    if (xmlNode.Attributes["name"] != null)
                    {
                        mainTag.NamedTagsNames.Add(xmlNode.Attributes["name"].Value);
                        mainTag.NamedTags.Add(xmlNode.Attributes["name"].Value, cTag);
                    }

                    SetAttributes(cTag, xmlNode);
                }

                // Check named children
                for (int n = 0; n < xmlNode.ChildNodes.Count; ++n)
                {
                    if (xmlNode.ChildNodes[n].NodeType == XmlNodeType.Element && cTag.NamedTags.ContainsKey(xmlNode.ChildNodes[n].Name))
                    {
                        SetAttributes(cTag.NamedTags[xmlNode.ChildNodes[n].Name], xmlNode.ChildNodes[n]);
                        cTag.NamedTags[xmlNode.ChildNodes[n].Name].XmlNode = xmlNode.ChildNodes[n];

                        // Check further naming on named tags
                        if (xmlNode.ChildNodes[n].Attributes["name"] != null)
                        {
                            mainTag.NamedTagsNames.Add(xmlNode.ChildNodes[n].Attributes["name"].Value);
                            mainTag.NamedTags.Add(xmlNode.ChildNodes[n].Attributes["name"].Value, cTag.NamedTags[xmlNode.ChildNodes[n].Name]);
                        }

                        // Set override children
                        if (xmlNode.ChildNodes[n].ChildNodes.Count > 0)
                        {
                            cTag.NamedTags[xmlNode.ChildNodes[n].Name].Children.Clear();
                            for (int m = 0; m < xmlNode.ChildNodes[n].ChildNodes.Count; ++m)
                            {
                                xTag ccTag = MakeTemplateNode(xmlNode.ChildNodes[n].ChildNodes[m], mainTag, cTag.NamedTags[xmlNode.ChildNodes[n].Name], rootTag, false);
                                if (ccTag != null)
                                {
                                    cTag.NamedTags[xmlNode.ChildNodes[n].Name].Children.Add(ccTag);
                                }
                            }
                        }
                    }
                    else if (mainTag != null && xmlNode.ChildNodes[n].NodeType == XmlNodeType.Element && xmlNode.ChildNodes[n].Name == "data")
                    {
                        MakeTemplateNode(xmlNode.ChildNodes[n], mainTag, cTag, rootTag, false);
                    }
                }

                return cTag;
            }
            else if (xmlNode.NodeType == XmlNodeType.Element && xmlNode.Name == "phrase") // Phrase
            {
                if (xmlNode.Attributes["library"] != null)
                    LoadLibrary(xmlNode.Attributes["src"].Value);

                if (parentTag.Children.Count == 0 ||
                    parentTag.Children[parentTag.Children.Count - 1].TagName != "#text")
                {
                    if (xmlNode.FirstChild != null)
                    {
                        xTag tag = new xTag();
                        tag.TagName = "#text";
                        tag.Text = xmlNode.FirstChild.Value;
                        tag.DefaultText = tag.Text;
                        tag.PhraseId = xmlNode.Attributes["id"].Value;
                        tag.ParentTag = parentTag;
                        return tag;
                    }
                }
                else
                {
                    parentTag.Children[parentTag.Children.Count - 1].Text += xmlNode.FirstChild.Value;
                    parentTag.Children[parentTag.Children.Count - 1].PhraseId = xmlNode.Attributes["id"].Value;
                    parentTag.Children[parentTag.Children.Count - 1].DefaultText =
                        parentTag.Children[parentTag.Children.Count - 1].Text;
                }
            }
            else if (xmlNode.NodeType == XmlNodeType.Element && xmlNode.Name == "data") // Data
            {
                // Domain and version dependencies
                if (IsOkToRender(xmlNode, parentTag.Version))
                {
                    XmlNode dataNode = xmlNode.FirstChild;
                    while (dataNode != null && dataNode.NodeType != XmlNodeType.CDATA)
                        dataNode = dataNode.NextSibling;

                    if (xmlNode.Attributes["type"] != null && xmlNode.Attributes["type"].Value == "text/attribute" &&
                            xmlNode.Attributes["name"] != null)
                    {
                        if (dataNode == null)
                        {
                            throw new InvalidOperationException("The data values must be wrapped by a CDATA section.");
                        }

                        // Set attribute
                        SetAttribute(parentTag, xmlNode.Attributes["name"].Value.ToLowerInvariant(), dataNode.Value);
                    }
                    else if (xmlNode.Attributes["type"] != null && xmlNode.Attributes["type"].Value == "text/name-value" &&
                            xmlNode.Attributes["name"] != null)
                    {
                        if (xmlNode.Attributes["src"] != null)
                        {
                            string val = GetUriContents(xmlNode.Attributes["src"].Value, true);

                            parentTag.Data[xmlNode.Attributes["name"].Value] = val;
                            parentTag.DefaultData[xmlNode.Attributes["name"].Value] = val;
                            if (parentTag.Attributes.ContainsKey(xmlNode.Attributes["name"].Value))
                            {
                                parentTag.Attributes[xmlNode.Attributes["name"].Value] = val;
                                parentTag.DefaultAttributes[xmlNode.Attributes["name"].Value] = val;
                            }
                        }
                        else if (dataNode != null)
                        {
                            parentTag.Data[xmlNode.Attributes["name"].Value] = dataNode.Value;
                            parentTag.DefaultData[xmlNode.Attributes["name"].Value] = dataNode.Value;
                            if (parentTag.Attributes.ContainsKey(xmlNode.Attributes["name"].Value))
                            {
                                parentTag.Attributes[xmlNode.Attributes["name"].Value] = dataNode.Value;
                                parentTag.DefaultAttributes[xmlNode.Attributes["name"].Value] = dataNode.Value;
                            }
                        }
                        else
                        {
                            parentTag.Data.Add(xmlNode.Attributes["name"].Value, null);
                            parentTag.DefaultData.Add(xmlNode.Attributes["name"].Value, null);
                        }
                    }
                    else if (xmlNode.Attributes["type"] != null && xmlNode.Attributes["type"].Value == "text/javascript-events")
                    {
                        if (!this.TemplateExternalServerOrigin.Contains(parentTag.RootTag.xTemplate))
                        {
                            if (xmlNode.Attributes["src"] != null)
                            {
                                var link = xmlNode.Attributes["src"].Value; // FixPathToWebPath(xmlNode.Attributes["src"].Value);
                                var linkContents = GetUriContents(link, true);
                                if (!string.IsNullOrWhiteSpace(linkContents))
                                {
                                    parentTag.EventsData = linkContents;
                                }
                            }
                            else if (dataNode != null)
                            {
                                parentTag.EventsData = !string.IsNullOrWhiteSpace(dataNode.Value) ? dataNode.Value : string.Empty;
                            }
                        }
                    }
                    else if (xmlNode.Attributes["type"] != null && xmlNode.Attributes["type"].Value == "text/javascript")
                    {
                        if (!this.TemplateExternalServerOrigin.Contains(parentTag.RootTag.xTemplate))
                        {
                            if (xmlNode.Attributes["src"] != null)
                            {
                                string link = FixPathToWebPath(xmlNode.Attributes["src"].Value);
                                if (JsLinks.IndexOf(link) == -1)
                                {
                                    JsLinks.Add(link);
                                }
                            }
                            else if (dataNode != null)
                            {
                                JsText.Add(dataNode.Value);
                            }
                        }
                    }
                    else if (xmlNode.Attributes["type"] != null && xmlNode.Attributes["type"].Value == "text/css")
                    {
                        if (xmlNode.Attributes["src"] != null)
                        {
                            var link = FixPathToWebPath(xmlNode.Attributes["src"].Value);
                            if (CssLinks.IndexOf(link) == -1)
                            {
                                CssLinks.Add(link);
                            }
                        }
                        else if (dataNode != null)
                        {
                            CssText.Add(dataNode.Value);
                        }
                    }
                    else if (xmlNode.Attributes["type"] != null && xmlNode.Attributes["type"].Value == "text/plain")
                    {
                        if (dataNode == null)
                        {
                            throw new InvalidOperationException("The data values must be wrapped by a CDATA section.");
                        }

                        xTag tag = new xTag();
                        tag.TagName = "#text";
                        tag.DefaultText = tag.Text = dataNode.Value;
                        tag.ParentTag = parentTag;
                        return tag;
                    }
                    //else if (OnCustomData != null)
                    //{
                    //    OnCustomData(parentTag, xmlNode, dataNode);
                    //}
                }
            }
            else if (xmlNode.NodeType == XmlNodeType.Element && xmlNode.Name.ToLowerInvariant() == "script")
            {
                // Script tags are not allowed
            }
            else if (xmlNode.NodeType == XmlNodeType.Element && xmlNode.Name.ToLowerInvariant() == "iframe")
            {
                // IFrames are not allowed
            }
            else if (xmlNode.NodeType == XmlNodeType.Element) // Normal
            {
                if (IsOkToRender(xmlNode, parentTag, mainTag))
                {
                    xTag tag = new xTag();
                    tag.TagName = xmlNode.Name.ToLowerInvariant();
                    tag.ParentTag = parentTag;

                    if (mainTag == null)
                    {
                        tag.xTemplate = xmlNode.Name;
                        tag.TagName = "div";

                        mainTag = tag;
                    }
                    else
                        tag.MainTag = mainTag;

                    if (rootTag != null)
                        tag.RootTag = rootTag;
                    else
                    {
                        tag.RootTag = rootTag = tag;
                    }

                    if (xmlNode.Attributes["name"] != null)
                    {
                        mainTag.NamedTagsNames.Add(xmlNode.Attributes["name"].Value);
                        mainTag.NamedTags.Add(xmlNode.Attributes["name"].Value, tag);
                    }

                    SetAttributes(tag, xmlNode);
                    tag.XmlNode = xmlNode;

                    if (tag.TagName == "body" && String.IsNullOrEmpty(tag.Id))
                        tag.Id = "__body";

                    if (String.IsNullOrEmpty(tag.DataSource) && computeChildren)
                    {
                        for (int n = 0; n < xmlNode.ChildNodes.Count; ++n)
                        {
                            xTag cTag = MakeTemplateNode(xmlNode.ChildNodes[n], mainTag, tag, rootTag, false);
                            if (cTag != null)
                            {
                                cTag.ParentIndex = tag.Children.Count;
                                if (!String.IsNullOrEmpty(tag.Text))
                                    throw new Exception("This element has both children and text; this is not allowed. Template: '" + mainTag.xTemplate + "'. Tag name: '" + tag.TagName + "'");
                                tag.Children.Add(cTag);
                            }
                        }
                    }

                    return tag;
                }
            }
            else if (xmlNode.NodeType == XmlNodeType.Text || xmlNode.NodeType == XmlNodeType.CDATA) // Text
            {
                if (parentTag.Children.Count == 0 ||
                    parentTag.Children[parentTag.Children.Count - 1].TagName != "#text")
                {
                    xTag tag = new xTag();
                    tag.TagName = "#text";
                    tag.Text = xmlNode.Value;
                    tag.DefaultText = tag.Text;
                    tag.ParentTag = parentTag;
                    return tag;
                }
                else
                {
                    parentTag.Children[parentTag.Children.Count - 1].Text += xmlNode.Value;
                    parentTag.Children[parentTag.Children.Count - 1].DefaultText =
                        parentTag.Children[parentTag.Children.Count - 1].Text;
                }
            }

            return null;
        }

        public void SetAttributes(xTag tag, XmlNode xmlNode)
        {
            for (int n = 0; n < xmlNode.Attributes.Count; ++n)
            {
                string lowerName = xmlNode.Attributes[n].Name.ToLowerInvariant();
                SetAttribute(tag, lowerName, xmlNode.Attributes[n].Value);
            }
        }

        public void SetAttribute(xTag tag, string lowerName, string value)
        {
            if (lowerName == "mode")
            {
                switch (value.ToLowerInvariant())
                {
                    case "server": tag.Mode = xTagMode.Server; break;
                    case "browser": tag.Mode = xTagMode.Browser; break;
                }
            }
            else if (lowerName == "tag")
            {
                tag.TagName = value;
            }
            else if (lowerName == "id")
            {
                tag.Id = value;
            }
            else if (lowerName == "serverorigin")
            {
                tag.ServerOrigin = value;
            }
            else if (lowerName == "apikey")
            {
                tag.ApiKey = value;
            }
            else if (lowerName == "version")
            {
                tag.Version = value;
            }
            else if (lowerName == "datasource")
            {
                tag.DataSource = value;
            }
            else if (lowerName == "modetype")
            {
                tag.ModeType = value;
            }
            else if (lowerName == "lcid")
            {
                var lcid = -1;
                int.TryParse(value, out lcid);
                tag.LCID = lcid;
            }
            else if (lowerName == "href" && value.ToLowerInvariant().IndexOf("javascript:") > -1)
            {
                tag.Attributes[lowerName] = "#";
                tag.DefaultAttributes[lowerName] = "#";
            }
            else
            {
                if (!tag.Attributes.ContainsKey(lowerName))
                {
                    tag.Attributes.Add(lowerName, value);
                    tag.DefaultAttributes.Add(lowerName, value);
                }
                else
                {
                    tag.Attributes[lowerName] = value;
                    tag.DefaultAttributes[lowerName] = value;
                }
            }
        }

        public string GetUriContents(string uri, bool safeCrossDomain)
        {
            string requestUri = FixPathToFileOrHttpPath(uri);
            if (requestUri.IndexOf("http") == 0)
            {
                // Check for cross domain origin here
                try
                {
                    var wr = WebRequest.Create(requestUri);
                    var resp = wr.GetResponse();
                    var sr = new StreamReader(resp.GetResponseStream());
                    return sr.ReadToEnd();
                }
                catch (WebException)
                {
                    return null;
                }
            }

            return File.ReadAllText(requestUri);
        }

        public bool IsOkToRender(XmlNode node)
        {
            return (node.Attributes["port"] == null ||
                            node.Attributes["port"].Value == ContextInfo.Port.ToString()) &&
                        (node.Attributes["domain"] == null ||
                            node.Attributes["domain"].Value == ContextInfo.Host);
        }

        public bool IsOkToRender(XmlNode node, xTag parentTag, xTag mainTag)
        {
            string version = null;
            if (mainTag != null)
                version = mainTag.Version;

            if (String.IsNullOrEmpty(version) && parentTag != null)
                version = parentTag.Version;

            return IsOkToRender(node, version);
        }

        public bool IsOkToRender(XmlNode node, string version)
        {
            return (node.Attributes["version"] == null ||
                            node.Attributes["version"].Value == version) &&
                        (node.Attributes["port"] == null ||
                            node.Attributes["port"].Value == ContextInfo.Port.ToString()) &&
                        (node.Attributes["domain"] == null ||
                            node.Attributes["domain"].Value == ContextInfo.Host);
        }

        #endregion

        public static string xTagsVersion()
        {
            return "0.7.3.9";
        }
    }
}
