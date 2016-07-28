using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Sharp.Xmpp
{
    /// <summary>
    /// Provides a factory method for creating XmlElement instances and adds
    /// a couple of useful shortcut extensions to the XmlElement class.
    /// </summary>
    internal static class Xml
    {
        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="namespace">The namespace of the element.</param>
        /// <returns>An initialized instance of the XmlElement class.</returns>
        /// <exception cref="ArgumentNullException">The name parameter is null.</exception>
        /// <exception cref="ArgumentException">The name parameter is the
        /// empty string.</exception>
        /// <exception cref="XmlException">The name or the namespace parameter
        /// is invalid.</exception>
        public static XElement Element(XNamespace ns, string tagname)
        {
            return new XElement(ns + tagname);
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="namespace">The namespace of the element.</param>
        /// <returns>An initialized instance of the XmlElement class.</returns>
        /// <exception cref="ArgumentNullException">The name parameter is null.</exception>
        /// <exception cref="ArgumentException">The name parameter is the
        /// empty string.</exception>
        /// <exception cref="XmlException">The name or the namespace parameter
        /// is invalid.</exception>
        public static XElement Element(string ns, string tagname)
        {
            return new XElement(string.Concat("{", ns, "}", tagname));
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        public static XElement Element(string ns, string tagname, object content)
        {
            return new XElement(string.Concat("{", ns, "}", tagname), content);
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        public static XElement Element(string ns, string tagname, params object[] content)
        {
            return new XElement(string.Concat("{", ns, "}", tagname), content);
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        public static XElement Element(string ns, string prefix, string tagname)
        {
            return new XElement(string.Concat("{", ns, "}", tagname), new XAttribute(XNamespace.Xmlns + prefix, ns));
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        public static XElement Element(string ns, string prefix, string tagname, object content)
        {
            return new XElement(string.Concat("{", ns, "}", tagname), new XAttribute(XNamespace.Xmlns + prefix, ns));
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        public static XElement Element(string ns, string prefix, string tagname, params object[] content)
        {
            return new XElement(string.Concat("{", ns, "}", tagname), new XAttribute(XNamespace.Xmlns + prefix, ns));
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        public static XElement Element(XElement other)
        {
            return new XElement(other);
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        public static XElement Element(XName name)
        {
            return new XElement(name);
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        public static XElement Element(XName name, object content)
        {
            return new XElement(name, content);
        }

        /// <summary>
        /// Creates a new XmlElement instance.
        /// </summary>
        public static XElement Element(XName name, params object[] content)
        {
            return new XElement(name, content);
        }
        
        /// <summary>
        /// Adds the specified element to the end of the list of child nodes, of
        /// this node.
        /// </summary>
        /// <param name="e">The XmlElement instance the method is invoked for.</param>
        /// <param name="child">The node to add.</param>
        /// <returns>A reference to the XmlElement instance.</returns>
        public static XElement Child(this XElement e, XElement child)
        {
            e.Add(child);
            return e;
        }

        /// <summary>
        /// Sets the value of the attribute with the specified name.
        /// </summary>
        /// <param name="e">The XmlElement instance the method is invoked for.</param>
        /// <param name="name">The name of the attribute to create or alter.</param>
        /// <param name="value">The value to set for the attribute.</param>
        /// <returns>A reference to the XmlElement instance.</returns>
        public static XElement Attr(this XElement e, string name, string value)
        {
            e.SetAttributeValue(name, value);
            return e;
        }

        /// <summary>
        /// Sets the value of the attribute with the specified name.
        /// </summary>
        /// <param name="e">The XmlElement instance the method is invoked for.</param>
        /// <param name="name">The name of the attribute to create or alter.</param>
        /// <param name="value">The value to set for the attribute.</param>
        /// <returns>A reference to the XmlElement instance.</returns>
        public static XElement Attr(this XElement e, XNamespace ns, string name, string value)
        {
            e.SetAttributeValue(ns + name, value);
            return e;
        }

        /// <summary>
        /// Adds the specified text to the end of the list of child nodes, of
        /// this node.
        /// </summary>
        /// <param name="e">The XmlElement instance the method is invoked for.</param>
        /// <param name="text">The text to add.</param>
        /// <returns>A reference to the XmlElement instance.</returns>
        public static XElement Text(this XElement e, string text)
        {
            e.Value = text;
            return e;
        }

        /// <summary>
        /// Serializes the XmlElement instance into a string.
        /// </summary>
        /// <param name="e">The XmlElement instance the method is invoked for.</param>
        /// <param name="xmlDeclaration">true to include a XML declaration,
        /// otherwise false.</param>
        /// <param name="leaveOpen">true to leave the tag of an empty element
        /// open, otherwise false.</param>
        /// <returns>A textual representation of the XmlElement instance.</returns>
        public static string ToXmlString(this XElement e, bool xmlDeclaration = false, bool leaveOpen = false)
        {
            string xml = e.ToString();

            if (xmlDeclaration)
                xml = "<?xml version='1.0' encoding='UTF-8'?>" + xml;

            if (leaveOpen)
                return Regex.Replace(xml, "/>$", ">");

            return xml;
        }

        /// <summary>
        /// Get child node which matches specified name
        /// </summary>
        /// <param name="ele"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XElement GetNodeName(this XElement ele, string name)
        {
            foreach (var node in ele.Nodes())
            {
                XElement element = node as XElement;

                if (element != null && element.Name.LocalName == name)
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>
        /// Get an attribute value attached with node
        /// </summary>
        /// <param name="ele"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        public static string GetAttributeValue(this XElement ele, string attr)
        {
            foreach (var attribute in ele.Attributes())
            {
                if (attribute.Name == attr)
                {
                    return attribute.Value;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Set an attribute value attached with node
        /// </summary>
        /// <param name="ele"></param>
        /// <param name="attr"></param>
        public static void SetAttributeValue(this XElement ele, string attr, string value)
        {
            foreach (var attribute in ele.Attributes())
            {
                if (attribute.Name == attr)
                {
                    attribute.Value = value;
                    return;
                }
            }
        }

        /// <summary>
        /// Remove child nodes of current element 
        /// </summary>
        /// <param name="ele"></param>
        /// <param name="nodeName"></param>
        public static void RemoveChild(this XElement ele, string nodeName)
        {
            var nodeList = ele.Nodes();

            nodeList.Where((x) =>
            {
                var item = x as XElement;
                return (item != null && item.Name == nodeName);
            }).Remove();
        }

        /// <summary>
        /// Remove attribute of current element 
        /// </summary>
        /// <param name="ele"></param>
        /// <param name="nodeName"></param>
        public static void RemoveAttribute(this XElement ele, string attrName)
        {
            var nodeList = ele.Attributes();

            nodeList.Where((x) =>
            {
                return (x.Name == attrName);
            }).Remove();
        }

        /// <summary>
        /// Get collection of element that matched with specified tag name
        /// </summary>
        /// <param name="ele"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static IEnumerable<XElement> GetElementsByTagName(this XElement ele, string tagName)
        {
            var nodeList = ele.Nodes().Where((x) =>
            {
                var item = x as XElement;
                return (item.Name.LocalName == tagName);
            }).Cast<XElement>();

            return nodeList;
        }
    }
}