using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Sharp.Xmpp.StreamParser
{
    public class XmppXElement : XElement
    {
        #region Fields
        private readonly static string[] XmppRootNamespacesStrings;
        #endregion

        #region Constructors
        static XmppXElement()
        {
            XmppRootNamespacesStrings = new string[] { " xmlns=\"jabber:client\"", " xmlns=\"jabber:server\"", " xmlns=\"jabber:component:accept\"" };
        }

        public XmppXElement(XNamespace ns, string tagname)
            : base(ns + tagname)
        {
        }

        public XmppXElement(string ns, string tagname)
            : this(string.Concat("{", ns, "}", tagname))
        {
        }

        public XmppXElement(string ns, string tagname, object content)
            : this(string.Concat("{", ns, "}", tagname), content)
        {
        }

        public XmppXElement(string ns, string tagname, params object[] content)
            : this(string.Concat("{", ns, "}", tagname), content)
        {
        }

        public XmppXElement(string ns, string prefix, string tagname)
            : this(string.Concat("{", ns, "}", tagname), new XAttribute(XNamespace.Xmlns + prefix, ns))
        {
        }

        public XmppXElement(string ns, string prefix, string tagname, object content)
            : this(string.Concat("{", ns, "}", tagname), new XAttribute(XNamespace.Xmlns + prefix, ns))
        {
            Add(content);
        }

        public XmppXElement(string ns, string prefix, string tagname, params object[] content)
            : this(string.Concat("{", ns, "}", tagname), new XAttribute(XNamespace.Xmlns + prefix, ns))
        {
            Add(content);
        }

        public XmppXElement(XElement other) : base(other)
        {
        }

        public XmppXElement(XName name) : base(name)
        {
        }

        public XmppXElement(XName name, object content) : base(name, content)
        {
        }

        public XmppXElement(XName name, params object[] content) : base(name, content)
        {
        }

        public XmppXElement(XStreamingElement other) : base(other)
        {

        }
        #endregion

        #region Methods
        internal void AddFirstOrRemoveTag(string tagname, bool add)
        {
            AddOrRemoveTag(tagname, add, true);
        }

        internal void AddNameSpaceDeclaration(string prefix, string value)
        {
            SetAttributeValue(XNamespace.Xmlns + prefix, value);
        }

        /// <summary>
        /// Adds or removes the given empty tag.
        /// </summary>
        /// <param name="tagname">The tagname.</param>
        /// <param name="add">if set to <c>true</c> add, otherwise remove.</param>
        internal void AddOrRemoveTag(string tagname, bool add)
        {
            AddOrRemoveTag(tagname, add, false);
        }

        /// <summary>
        /// Adds or removes the given empty tag.
        /// </summary>
        /// <param name="tagname">The tagname.</param>
        /// <param name="add">if set to <c>true</c> [add].</param>
        /// <param name="addFirst">if set to <c>true</c> [add first].</param>
        internal void AddOrRemoveTag(string tagname, bool add, bool addFirst)
        {
            if (!add)
            {
                this.RemoveTag(tagname);
                return;
            }

            if (addFirst)
            {
                this.SetTagFirst(tagname);
                return;
            }

            this.SetTag(tagname);
        }

        /// <summary>
        /// Add a new empty childnode (fluent API).
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        public XmppXElement AddTag(string tagname)
        {
            return AddTag(tagname, null);
        }

        /// <summary>
        /// Adds a childnode (fluent API).
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <param name="content">the value of the new tag</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        public XmppXElement AddTag(string tagname, string content)
        {
            return AddTag(Name.Namespace, tagname, content);
        }

        /// <summary>
        /// Adds a childnode (fluent API).
        /// </summary>
        /// <param name="ns">the namespace of the new child node</param>
        /// <param name="tagname">the tagname</param>
        /// <param name="content">the value of the new tag</param>
        /// <returns>the new childnode</returns>
        public XmppXElement AddTag(string ns, string tagname, string content)
        {
            return AddTag((XNamespace)ns, tagname, content);
        }

        /// <summary>
        /// Adds a childnode (fluent API).
        /// </summary>
        /// <param name="ns"><see cref="T:System.Xml.Linq.XNamespace" /> of the new child node</param>
        /// <param name="tagname">the tagname</param>
        /// <param name="content">the value of the new tag</param>
        /// <returns>the new childnode</returns>
        public XmppXElement AddTag(XNamespace ns, string tagname, string content)
        {
            return DoAddTag(ns, tagname, content, false);
        }

        /// <summary>
        /// Add a new empty childnode as first child (fluent API).
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        public XmppXElement AddTagFirst(string tagname)
        {
            return AddTagFirst(tagname, null);
        }

        /// <summary>
        /// Adds a childnode as the first child (fluent API).
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <param name="content">the value of the new tag</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        public XmppXElement AddTagFirst(string tagname, string content)
        {
            return AddTagFirst(Name.Namespace, tagname, content);
        }

        /// <summary>
        /// Adds a childnode as the first child (fluent API).
        /// </summary>
        /// <param name="ns">the namespace of the new child node</param>
        /// <param name="tagname">the tagname</param>
        /// <param name="content">the value of the new tag</param>
        /// <returns>the new childnode</returns>
        public XmppXElement AddTagFirst(string ns, string tagname, string content)
        {
            return AddTagFirst((XNamespace)ns, tagname, content);
        }

        /// <summary>
        /// Adds the tag as the first child Element (fluent API).
        /// </summary>
        /// <param name="ns">The ns.</param>
        /// <param name="tagname">The tagname.</param>
        /// <param name="content">The content.</param>        
        /// <returns></returns>
        public XmppXElement AddTagFirst(XNamespace ns, string tagname, string content)
        {
            return DoAddTag(ns, tagname, content, true);
        }

        /// <summary>
        /// Add child node
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="tagname"></param>
        /// <param name="content"></param>
        /// <param name="addFirst"></param>
        /// <returns></returns>
        internal XmppXElement DoAddTag(XNamespace ns, string tagname, string content, bool addFirst)
        {
            XmppXElement xmppXElement = new XmppXElement(ns, tagname);

            if (!addFirst)
            {
                Add(xmppXElement);
            }
            else
            {
                AddFirst(xmppXElement);
            }

            if (content != null)
            {
                xmppXElement.Value = content;
            }

            return xmppXElement;
        }

        internal XElement DoSetTag(XNamespace ns, string tagname, string content, bool addFirst)
        {
            XElement tagXElement = GetTagXElement(ns.NamespaceName, tagname);

            if (tagXElement == null)
            {
                tagXElement = new XmppXElement(ns, tagname);

                if (!addFirst)
                {
                    Add(tagXElement);
                }
                else
                {
                    AddFirst(tagXElement);
                }
            }

            if (content != null)
            {
                tagXElement.Value = content;
            }

            return tagXElement;
        }

        /// <summary>
        /// Get a xml element
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="tagname"></param>
        /// <returns></returns>
        public XElement GetTagXElement(string ns, string tagname)
        {
            if (string.IsNullOrEmpty(ns))
            {
                return Element(tagname);
            }

            return GetTagXElement((XNamespace)ns, tagname);
        }

        /// <summary>
        /// Get a xml element
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="tagname"></param>
        /// <returns></returns>
        public XElement GetTagXElement(XNamespace xns, string tagname)
        {
            return Element(xns + tagname);
        }

        /// <summary>
        /// Get a xml element
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="tagname"></param>
        /// <returns></returns>
        public XElement GetTagXElement(string tagname)
        {
            return GetTagXElement(Name.NamespaceName, tagname);
        }

        /// <summary>
        /// Add a new datetime childnode
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        internal void SetTag(string ns, string tagname, DateTime date)
        {
            XNamespace xNamespace = ns;
            SetTag(xNamespace, tagname, date.ToString("yyyyMMdd"));
        }

        /// <summary>
        /// Add a new datetime childnode
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        internal void SetTag(string tagname, DateTime date)
        {
            SetTag(Name.Namespace, tagname, date);
        }

        /// <summary>
        /// Add a datetime childnode
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        internal void SetTag(XNamespace ns, string tagname, DateTime date)
        {
            SetTag(ns, tagname, date.ToString("yyyyMMdd"));
        }

        /// <summary>
        /// Add a new empty childnode
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        public XElement SetTag(string tagname)
        {
            return SetTag(tagname, null);
        }

        /// <summary>
        /// Adds a childnode
        /// </summary>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        public XElement SetTag(string tagname, string content)
        {
            return SetTag(Name.Namespace, tagname, content);
        }

        /// <summary>
        /// Adds a childnode
        /// </summary>
        /// <returns>the new childnode</returns>
        public XElement SetTag(string ns, string tagname, string content)
        {
            return SetTag((XNamespace)ns, tagname, content);
        }

        /// <summary>
        /// Adds a childnode
        /// </summary>
        /// <returns>the new childnode</returns>
        public XElement SetTag(XNamespace ns, string tagname, string content)
        {
            return DoSetTag(ns, tagname, content, false);
        }

        /// <summary>
        /// Adds a childnode
        /// </summary>
        /// <returns>the new childnode</returns>
        public XElement SetTag(string tagname, bool content)
        {
            return SetTag(Name.NamespaceName, tagname, content);
        }

        /// <summary>
        /// Adds a childnode
        /// </summary>
        /// <returns>the new childnode</returns>
        public XElement SetTag(string ns, string tagname, bool content)
        {
            return SetTag(ns, tagname, (content ? "true" : "false"));
        }

        /// <summary>
        /// Adds a childnode
        /// </summary>
        /// <returns>the new childnode</returns>
        public XElement SetTag(string ns, string tagname, int content)
        {
            return SetTag(ns, tagname, content.ToString());
        }

        /// <summary>
        /// Adds a childnode
        /// </summary>
        /// <returns>the new childnode</returns>
        public XElement SetTag(string tagname, int content)
        {
            return SetTag(tagname, content.ToString());
        }

        /// <summary>
        /// Adds a childnode
        /// </summary>
        /// <returns>the new childnode</returns>
        public void SetTag(string tagname, double value, IFormatProvider formatProvider)
        {
            SetTag(tagname, value.ToString(formatProvider));
        }

        /// <summary>
        /// Adds a childnode
        /// </summary>
        /// <returns>the new childnode</returns>
        public void SetTag(string tagname, double value)
        {
            NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
            numberFormatInfo.NumberGroupSeparator = ".";
            SetTag(tagname, value, numberFormatInfo);
        }

        /// <summary>
        /// Add a new empty childnode as first child.
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        public XElement SetTagFirst(string tagname)
        {
            return this.SetTagFirst(tagname, null);
        }

        /// <summary>
        /// Adds a childnode as the first child.
        /// </summary>
        /// <param name="tagname">the tagname</param>
        /// <param name="content">the value of the new tag</param>
        /// <returns>the new child node</returns>
        /// <remarks>the new element will use the namespace of its parent</remarks>
        public XElement SetTagFirst(string tagname, string content)
        {
            return SetTagFirst(Name.Namespace, tagname, content);
        }

        /// <summary>
        /// Adds a childnode as the first child
        /// </summary>
        /// <param name="ns">the namespace of the new child node</param>
        /// <param name="tagname">the tagname</param>
        /// <param name="content">the value of the new tag</param>
        /// <returns>the new childnode</returns>
        public XElement SetTagFirst(string ns, string tagname, string content)
        {
            return SetTagFirst((XNamespace)ns, tagname, content);
        }

        /// <summary>
        /// Adds the tag as the first child Element.
        /// </summary>
        /// <param name="ns">The ns.</param>
        /// <param name="tagname">The tagname.</param>
        /// <param name="content">The content.</param>        
        /// <returns></returns>
        public XElement SetTagFirst(XNamespace ns, string tagname, string content)
        {
            return DoSetTag(ns, tagname, content, true);
        }

        /// <summary>
        /// Remove child node
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="tagname"></param>
        public void RemoveTag(string tagname)
        {
            RemoveTag(Name.NamespaceName, tagname);
        }

        /// <summary>
        /// Remove child node
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="tagname"></param>
        public void RemoveTag(XNamespace xns, string tagname)
        {
            XElement tagXElement = GetTagXElement(xns, tagname);

            if (tagXElement != null)
            {
                tagXElement.Remove();
            }
        }
        
        /// <summary>
        /// Remove child node
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="tagname"></param>
        public void RemoveTag(string ns, string tagname)
        {
            XElement tagXElement = GetTagXElement(ns, tagname);

            if (tagXElement != null)
            {
                tagXElement.Remove();
            }
        }

        /// <summary>
        /// Get only the start Tag of the element
        /// </summary>
        /// <returns></returns>
        public string StartTag()
        {
            XmppXElement xmppXElement = new XmppXElement(this);
            Extensions.Remove(xmppXElement.Nodes());
            return xmppXElement.ToString(false).Replace("/>", ">");
        }

        /// <summary>
        /// Sets the text of the current node (fluent API).
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public XmppXElement Text(string text)
        {
            Value = text;
            return this;
        }
        
        /// <summary>
        /// Goes up one node in the tree to the parent for (fluent API).
        /// </summary>
        /// <returns></returns>
        public XmppXElement Up()
        {
            return Parent as XmppXElement;
        }

        /// <summary>
        /// Sets a "string" attribute
        /// </summary>
        /// <param name="name">attribute name</param>
        /// <param name="value">attribute value as integer</param>
        public XmppXElement SetAttribute(string attname, string val)
        {
            SetAttributeValue(attname, val);
            return this;
        }

        /// <summary>
        /// Sets a "integer" attribute
        /// </summary>
        /// <param name="name">attribute name</param>
        /// <param name="value">attribute value as integer</param>
        public XmppXElement SetAttribute(string name, int value)
        {
            SetAttribute(name, value.ToString());
            return this;
        }

        /// <summary>
        /// Sets a "long" attribute
        /// </summary>
        /// <param name="name">attribute name</param>
        /// <param name="value">attribute value as long</param>
        public XmppXElement SetAttribute(string name, long value)
        {
            SetAttribute(name, value.ToString());
            return this;
        }

        /// <summary>
        /// Sets a "boolean" attribute, the value is either 'true' or 'false'
        /// </summary>
        /// <param name="name">attribute name</param>
        /// <param name="val">attribute value as boolean</param>
        public XmppXElement SetAttribute(string name, bool val)
        {
            this.SetAttribute(name, (val ? "true" : "false"));
            return this;
        }

        /// <summary>
        /// Set a attribute of type Jid
        /// </summary>
        /// <param name="name">attribute name</param>
        /// <param name="jid">value of the attribute, or null to remove the attribute</param>
        public XmppXElement SetAttribute(string name, Jid jid)
        {
            if (jid == null)
            {
                this.RemoveAttribute(name);
            }
            else
            {
                SetAttribute(name, jid.ToString());
            }
            return this;
        }

        /// <summary>
        /// Set a "double" attribute with english number format
        /// </summary>
        /// <param name="name">attribute name</param>
        /// <param name="value">value of the attribute as double</param>
        public XmppXElement SetAttribute(string name, double value)
        {
            NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
            numberFormatInfo.NumberGroupSeparator = ".";
            SetAttribute(name, value, numberFormatInfo);
            return this;
        }

        /// <summary>
        /// Set a "double" attribute with the given FormatProvider
        /// </summary>
        /// <param name="name">attribute name</param>
        /// <param name="value">value of teh attribute as double</param>
        /// <param name="ifp">IFormatProvider</param>
        public XmppXElement SetAttribute(string name, double value, IFormatProvider formatProvider)
        {
            SetAttribute(name, value.ToString(formatProvider));
            return this;
        }

        /// <summary>
        /// Sets Attribute as base64 string
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public XmppXElement SetAttributeBase64(string name, byte[] value)
        {
            SetAttribute(name, Convert.ToBase64String(value));
            return this;
        }

        public virtual string ToString(bool indented)
        {
            StringBuilder stringBuilder = new StringBuilder();

            XmlWriterSettings xmlWriterSetting = new XmlWriterSettings();
            xmlWriterSetting.OmitXmlDeclaration = true;
            xmlWriterSetting.Indent = indented;
            XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, xmlWriterSetting);

            try
            {
                WriteTo(xmlWriter);
            }
            finally
            {
                if (xmlWriter != null)
                {
                    xmlWriter.Dispose();
                }
            }

            return stringBuilder.ToString();
        }
        #endregion
    }
}
