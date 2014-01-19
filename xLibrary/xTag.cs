namespace xLibrary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using Chains.Play;

    [Serializable]
    sealed public class xTag : SerializableSpecification
    {
        public xContext xContext;
        public string TagName = "div";
        public xTag ParentTag = null, MainTag = null, RootTag = null;
        public List<xTag> Children = new List<xTag>();
        public List<string> NamedTagsNames = new List<string>();
        public Dictionary<string, xTag> NamedTags = new Dictionary<string, xTag>();
        public Dictionary<string, string> Data = new Dictionary<string, string>();
        public string EventsData = null;
        public Dictionary<string, string> Attributes = new Dictionary<string, string>();
        public xTagMode Mode = xTagMode.Normal;
        public string ModeType = "Normal";
        public string xTemplate = null;
        public string Text = null;
        public string PhraseId = null;
        public int LCID = -1;
        public string Id = null;
        public string ServerOrigin = null;
        public string Session = null;
        public string ApiKey = null;
        public string Token = null;
        public int ParentIndex = -1;
        public string Version = null;
        public Dictionary<string, string> DefaultData = new Dictionary<string, string>();
        public Dictionary<string, string> DefaultAttributes = new Dictionary<string, string>();
        public string DefaultText = null;
        public object DataObject = null;
        public string DataSource = null;
        public XmlNode XmlNode = null;

        internal string argId = null;

        public string GetId()
        {
            if (!string.IsNullOrEmpty(this.Id))
                return this.Id;

            if (this.Attributes.ContainsKey("name"))
            {
                if (this.MainTag != null)
                    return this.MainTag.GetId() + ":" + this.Attributes["name"];

                return this.xTemplate + ":" + this.Attributes["name"];
            }

            if (this.Attributes.ContainsKey("class"))
            {
                if (this.MainTag != null)
                    return this.MainTag.GetId() + "." + this.Attributes["class"];

                return this.xTemplate + "." + this.Attributes["class"];
            }

            int childIndex = -1;
            if (this.ParentTag != null)
            {
                return this.ParentTag.GetId() + "." + this.ParentIndex;
            }

            return this.xTemplate + ".0";
        }

        public void Switch(string newTag)
        {
            this.Switch(xContext.Create(newTag));
        }

        public void Append(string newTag)
        {
            this.Append(xContext.Create(newTag));
        }

        public void Switch(xTag newTag)
        {
        }

        public void Append(xTag newTag)
        {
            this.Children.Add(newTag);
            newTag.ParentIndex = this.Children.Count;
            newTag.ParentTag = this;
            if (!string.IsNullOrEmpty(this.xTemplate))
            {
                newTag.MainTag = this;
            }
            else
            {
                newTag.MainTag = this.MainTag;
            }

            if (newTag.Attributes.ContainsKey("name"))
            {
                if (newTag.MainTag.NamedTags.ContainsKey(newTag.Attributes["name"]))
                {
                    newTag.MainTag.NamedTags[newTag.Attributes["name"]] = newTag;
                }
                else
                {
                    newTag.MainTag.NamedTags.Add(newTag.Attributes["name"], newTag);
                    newTag.MainTag.NamedTagsNames.Add(newTag.Attributes["name"]);
                }
            }

            newTag.RootTag = this.RootTag;
        }

        public void Delete()
        {
            Delete(false);
        }

        void Delete(bool propagated)
        {
            if (this.Attributes.ContainsKey("name"))
            {
                if (this.MainTag.NamedTags.ContainsKey(this.Attributes["name"]))
                {
                    this.MainTag.NamedTags.Remove(this.Attributes["name"]);
                }
            }

            for (int n = 0; n < this.Children.Count; ++n)
            {
                this.Children[n].Delete(true);
            }

            if (!propagated)
            {
                this.ParentTag.Children.RemoveAt(this.ParentIndex);
                for (int n = 0; n < this.ParentTag.Children.Count; ++n)
                {
                    this.ParentTag.Children[n].ParentIndex = n;
                }
            }
        }

        public xTag GetTagById(string id)
        {
            if (this.GetId() == id)
                return this;

            for (int i = 0; i < this.Children.Count; ++i)
            {
                xTag tag = this.Children[i].GetTagById(id);
                if (tag != null)
                    return tag;
            }

            return null;
        }

        public List<xTag> GetTagsByTemplateName(string templateName)
        {
            var tags = new List<xTag>();

            if (this.xTemplate == templateName)
                tags.Add(this);

            for (int i = 0; i < this.Children.Count; ++i)
            {
                tags.AddRange(this.Children[i].GetTagsByTemplateName(templateName));
            }

            return tags;
        }

        public List<xTag> GetTagsByTagName(string tagName)
        {
            var tags = new List<xTag>();

            if (this.TagName == tagName)
                tags.Add(this);

            for (int i = 0; i < this.Children.Count; ++i)
            {
                tags.AddRange(this.Children[i].GetTagsByTagName(tagName));
            }

            return tags;
        }

        public override int DataStructureVersionNumber
        {
            get
            {
                return xContext.xTagsVersion().GetHashCode();
            }
        }
    }
}
