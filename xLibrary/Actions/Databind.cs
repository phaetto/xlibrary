namespace xLibrary.Actions
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;

    using Chains;
    using Newtonsoft.Json.Linq;

    public class Databind : IChainableAction<xTagContext, xTagContext>
    {
        private static Regex bindingRE = new Regex(@"#\{([^\{\}]*?)\}", RegexOptions.Compiled);
        private static Regex propertiesRE = new Regex(@"this(\.([a-zA-Z0-9\.]+)\(\)|\[['""]([a-zA-Z0-9\.]+)['""]\](\(\))?)", RegexOptions.Compiled);

        public readonly Func<xTag, object> customDataSource;
        public readonly object dataObject;

        public Databind(object dataObject = null, Func<xTag, object> customDataSource = null)
        {
            this.dataObject = dataObject;
            this.customDataSource = customDataSource;
        }

        public xTagContext Act(xTagContext context)
        {
            context.xTag.DataObject = dataObject;
            DatabindMethod(context.xTag, context.Parent);

            return context;
        }

        object GetDataObject(xTag xtag)
        {
            if (xtag.DataObject == null && xtag.ParentTag != null)
            {
                return GetDataObject(xtag.ParentTag);
            }

            return xtag.DataObject;
        }

        void DatabindMethod(xTag xtag, xContext context)
        {
            var o = GetDataObject(xtag);

            if (o == null)
            {
                return;
            }

            // Bind attributes
            foreach (var key in xtag.DefaultAttributes.Keys)
            {
                if (!string.IsNullOrEmpty(xtag.DefaultAttributes[key]) && bindingRE.IsMatch(xtag.DefaultAttributes[key]))
                {
                    xtag.Attributes[key] = BindText(xtag.DefaultAttributes[key], o);
                }
            }

            // Bind data-values
            foreach (var key in xtag.DefaultData.Keys)
            {
                if (!string.IsNullOrEmpty(xtag.DefaultData[key]) && bindingRE.IsMatch(xtag.DefaultData[key]))
                {
                    xtag.Data[key] = BindText(xtag.DefaultData[key], o);
                }
            }

            // Bind text
            if (xtag.TagName == "#text" && bindingRE.IsMatch(xtag.DefaultText))
                xtag.Text = BindText(xtag.DefaultText, o);

            var boundObject = o;
            if (!string.IsNullOrEmpty(xtag.DataSource) && xtag.DataSource != "xtag")
            {
                boundObject = null;
                if (customDataSource != null)
                {
                    boundObject = customDataSource(xtag);
                }

                if (boundObject == null)
                {
                    if (o is IDictionary)
                    {
                        if ((o as IDictionary).Contains(xtag.DataSource))
                        {
                            boundObject = (o as IDictionary)[xtag.DataSource];
                        }
                    }
                    else if (o is XmlNode)
                    {
                        if (o is XmlDocument)
                        {
                            o = (o as XmlDocument).DocumentElement;
                        }

                        boundObject = xtag.DataSource == "ChildNodes"
                            ? (o as XmlNode).ChildNodes
                            : (o as XmlNode).SelectNodes(xtag.DataSource);
                    }
                    else
                    {
                        var pi = o.GetType().GetProperty(xtag.DataSource);
                        if (pi != null)
                        {
                            boundObject = pi.GetValue(o, null);
                        }
                        else
                        {
                            var fi = o.GetType().GetField(xtag.DataSource);
                            if (fi != null)
                            {
                                boundObject = fi.GetValue(o);
                            }
                        }
                    }
                }
            }

            if (boundObject != null && xtag.XmlNode != null)
            {
                 // Bind/Create children
                if (boundObject is IEnumerable && !(boundObject is XmlNode)
                    && !(boundObject is IDictionary) && !(boundObject is JObject))
                {
                    // Delete children and remake
                    while (xtag.Children.Count > 0)
                    {
                        xtag.Children[xtag.Children.Count - 1].Delete();
                    }

                    var enu = (boundObject as IEnumerable).GetEnumerator();

                    while (enu.MoveNext())
                    {
                        if (enu.Current == null)
                        {
                            continue;
                        }

                        for (var n = 0; n < xtag.XmlNode.ChildNodes.Count; ++n)
                        {
                            if (xtag.XmlNode.NodeType == XmlNodeType.Element
                                && xtag.XmlNode.Name.ToLowerInvariant() != "data")
                            {
                                var cTag = context.MakeTemplateNode(
                                    xtag.XmlNode.ChildNodes[n],
                                    xtag.MainTag ?? xtag,
                                    xtag,
                                    xtag.RootTag,
                                    false);
                                if (cTag != null)
                                {
                                    cTag.ParentIndex = xtag.Children.Count;
                                    cTag.DataObject = enu.Current;
                                    xtag.Children.Add(cTag);
                                }
                            }
                        }
                    }
                }
                // Do not remake if a single object exists (has been already created)
            }
            else if (boundObject == null)
            {
                // Delete children
                while (xtag.Children.Count > 0)
                {
                    xtag.Children[xtag.Children.Count - 1].Delete();
                }
            }

            foreach (var tag in xtag.Children)
            {
                DatabindMethod(tag, context);
            }
        }

        static string BindText(string text, object obj)
        {
            var newText = text;

            var bindings = bindingRE.Match(newText);
            while (bindings.Groups.Count > 1)
            {
                var value = bindings.Groups[1].Value;
                var props = propertiesRE.Match(value);

                while (props.Groups.Count > 1)
                {
                    string property = props.Groups[2].Value;
                    if (string.IsNullOrEmpty(property))
                    {
                        property = props.Groups[3].Value;
                    }

                    object oval = null;

                    if (obj is IDictionary)
                    {
                        if ((obj as IDictionary).Contains(property))
                        {
                            oval = (obj as IDictionary)[property];
                        }
                    }
                    else if (obj is XmlNode)
                    {
                        var xn = obj as XmlNode;
                        if (xn is XmlDocument)
                        {
                            xn = (xn as XmlDocument).DocumentElement;
                        }

                        switch (property)
                        {
                            case "InnerText":
                                oval = xn.InnerText;
                                break;
                            case "Text":
                            {
                                var nodeText =
                                    xn.ChildNodes.Cast<XmlNode>()
                                      .Where(xnc => xnc.NodeType == XmlNodeType.Text)
                                      .Aggregate(string.Empty, (current, xnc) => current + xnc.InnerText);
                                oval = nodeText;
                            }
                                break;
                            default:
                                if (xn.Attributes[property] != null)
                                {
                                    oval = xn.Attributes[property].Value;
                                }
                                else if (xn.SelectSingleNode(property) != null)
                                {
                                    var xn2 = xn.SelectSingleNode(property);
                                    var nodeText =
                                        xn2.ChildNodes.Cast<XmlNode>()
                                           .Where(xnc => xnc.NodeType == XmlNodeType.Text)
                                           .Aggregate(string.Empty, (current, xnc) => current + xnc.InnerText);
                                    oval = nodeText;
                                }
                                break;
                        }
                    }
                    else if (obj is JObject)
                    {
                        var jobj = obj as JObject;
                        oval = jobj[property];
                    }
                    else
                    {
                        var pi = obj.GetType().GetProperty(property);
                        if (pi != null)
                        {
                            oval = pi.GetValue(obj, null);
                        }
                        else
                        {
                            var fi = obj.GetType().GetField(property);
                            if (fi != null)
                            {
                                oval = fi.GetValue(obj);
                            }
                        }
                    }

                    if (oval != null)
                    {
                        value = value.Replace(props.Groups[0].Value, oval.ToString());
                        props = propertiesRE.Match(value);
                    }
                    else
                    {
                        value = value.Replace(props.Groups[0].Value, string.Empty);
                        props = propertiesRE.Match(value);
                    }
                }

                newText = newText.Replace(bindings.Groups[0].Value, value);
                bindings = bindingRE.Match(newText);
            }

            return newText;
        }
    }
}
