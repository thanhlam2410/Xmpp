using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Sharp.Xmpp.StreamParser
{
    public class XmppNode
    {
        /// <summary>
        /// Current node name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Contain xml data for this node
        /// </summary>
        public string Xml { get; set; }

        /// <summary>
        /// Indicate current xml node type
        /// </summary>
        public XmlNodeType NodeType { get; set; }
    }
}
