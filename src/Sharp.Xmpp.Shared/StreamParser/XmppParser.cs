using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Sharp.Xmpp.StreamParser
{
    public class XmppParser
    {
        #region Constant
        public static Regex RegexXMLBlock = new Regex(@"^[^\<\>+\>]+|\<[^\<\>]+\>", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexTextPlain = new Regex(@"^[^\<\>+\>]+", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexDeclaration = new Regex(@"\<\?xml [^\<\>]* \?\>", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexComment = new Regex(@"\< \s* !-- [^\<\>]* \>", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexEndElement = new Regex(@"\<\/ (?<name>[^\<\>]+) \>", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexStartElement = new Regex(@"\< (?<name>\S+) [^\<\>]* \>", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexCompleteElement = new Regex(@"\< (?<name>\S+) [^\<\>]* \/\>", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        #endregion

        #region Fields
        private Queue<XmppNode> _nodeQueue;
        private List<XmppTree> _xmppMessageList;
        private Stack<string> _opening;
        private string _currentNodeName;
        private readonly object _syncLock = new object(); //Make sure parser is thread-safe
        #endregion

        #region Event

        #endregion

        #region Methods
        public XmppParser()
        {
            _nodeQueue = new Queue<XmppNode>();
            _xmppMessageList = new List<XmppTree>();
            _opening = new Stack<string>();
        }

        /// <summary>
        /// Write received xmpp data to parser
        /// </summary>
        public XmppTree[] Write(string xml)
        {
            lock (_syncLock)
            {
                string rootXml = xml;
                XmppNode node = GetBlock(ref rootXml);

                while (node != null)
                {
                    if ((node.NodeType == XmlNodeType.XmlDeclaration) || (node.NodeType == XmlNodeType.Comment) || (node.NodeType == XmlNodeType.Whitespace))
                    {
                        node = GetBlock(ref rootXml);
                        continue;
                    }

                    if ((node.NodeType == XmlNodeType.EndElement) && (node.Name == "stream:stream"))
                    {

                    }
                    else if (node.Name == "stream:stream")
                    {

                    }
                    else if ((node.NodeType == XmlNodeType.EndElement) && (string.IsNullOrEmpty(_currentNodeName)))
                    {
                        XmppNode[] nodeArr = new XmppNode[1];
                        nodeArr[0] = node;

                        ProcessXmppTree(nodeArr);
                    }
                    else
                    {
                        //Receive start node or end-node
                        if (string.IsNullOrEmpty(_currentNodeName))
                        {
                            //Must be a start-tag
                            _currentNodeName = node.Name;
                            _nodeQueue.Enqueue(node);
                            _opening.Push(node.Name);
                        }
                        else
                        {
                            if (node.NodeType == XmlNodeType.Element)
                            {
                                _nodeQueue.Enqueue(node);
                                _opening.Push(node.Name);
                            }
                            else if (node.NodeType == XmlNodeType.EndElement)
                            {
                                if (_opening.Peek() == node.Name)
                                {
                                    _opening.Pop();
                                }

                                _nodeQueue.Enqueue(node);

                                if (_opening.Count == 0)
                                {
                                    _currentNodeName = null;

                                    XmppNode[] cloneQueue = new XmppNode[_nodeQueue.Count];
                                    _nodeQueue.CopyTo(cloneQueue, 0);

                                    ProcessXmppTree(cloneQueue);
                                    _nodeQueue.Clear();
                                }
                            }
                            else if (node.NodeType == XmlNodeType.Text)
                            {
                                _nodeQueue.Enqueue(node);
                            }
                        }
                    }

                    node = GetBlock(ref rootXml);
                }

                //if out of data, reset
                _currentNodeName = null;
                _opening.Clear();
                _nodeQueue.Clear();

                XmppTree[] nodeList = new XmppTree[_xmppMessageList.Count];
                _xmppMessageList.CopyTo(nodeList, 0);
                _xmppMessageList.Clear();

                return nodeList;
            }
        }

        /// <summary>
        /// Get xml block
        /// </summary>
        /// <param name="rootXml"></param>
        /// <returns></returns>
        private XmppNode GetBlock(ref string rootXml)
        {
            rootXml = rootXml.TrimEnd();

            if (rootXml.Length <= 0)
                return null;

            if (rootXml[rootXml.Length - 1] != '>') // make sure we have a full xml fragment before starting
                return null;

            Match match = RegexXMLBlock.Match(rootXml, 0);

            if (match.Success == true)
            {
                int endIndex = match.Index + match.Length;
                rootXml = rootXml.Remove(0, endIndex);

                return ParseXMLNode(match.Value);
            }

            return null;
        }

        /// <summary>
        /// Parse xml data to XMPP node
        /// </summary>
        /// <param name="rootXml"></param>
        /// <returns></returns>
        private XmppNode ParseXMLNode(string rootXml)
        {
            Match matchman = RegexDeclaration.Match(rootXml);
            XmppNode xmppNode = new XmppNode();
            xmppNode.Xml = rootXml;

            if (matchman.Success == true)
            {
                xmppNode.NodeType = XmlNodeType.XmlDeclaration;
                return xmppNode;
            }

            matchman = RegexComment.Match(rootXml);
            if (matchman.Success == true)
            {
                xmppNode.NodeType = XmlNodeType.Comment;
                return xmppNode;
            }

            matchman = RegexEndElement.Match(rootXml);
            if (matchman.Success == true)
            {
                xmppNode.NodeType = XmlNodeType.EndElement;
                xmppNode.Name = matchman.Groups["name"].Value;

                return xmppNode;
            }

            matchman = RegexCompleteElement.Match(rootXml);
            if (matchman.Success == true)
            {
                xmppNode.NodeType = XmlNodeType.EndElement;
                xmppNode.Name = matchman.Groups["name"].Value;

                return xmppNode;
            }

            matchman = RegexStartElement.Match(rootXml);
            if (matchman.Success == true)
            {
                xmppNode.NodeType = XmlNodeType.Element;
                xmppNode.Name = matchman.Groups["name"].Value;

                return xmppNode;
            }

            matchman = RegexTextPlain.Match(rootXml);
            if (matchman.Success == true)
            {
                xmppNode.NodeType = XmlNodeType.Text;
                return xmppNode;
            }

            // Don't care about anything else
            return null;
        }

        /// <summary>
        /// Parse node queue to xmpp tree
        /// </summary>
        private void ProcessXmppTree(XmppNode[] nodeQueue)
        {
            string xml = string.Empty;

            foreach (var item in nodeQueue)
            {
                xml += item.Xml;
            }

            XmppTree message = new XmppTree();
            message.TreeName = nodeQueue[0].Name;
            message.Xml = xml;

            _xmppMessageList.Add(message);
        }
        #endregion
    }
}
