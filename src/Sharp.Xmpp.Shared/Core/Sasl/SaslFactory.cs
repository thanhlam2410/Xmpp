using Sharp.Xmpp.Core.Sasl.Mechanisms;
using System;
using System.Collections.Generic;

namespace Sharp.Xmpp.Core.Sasl
{
    /// <summary>
    /// A factory class for producing instances of Sasl mechanisms.
    /// </summary>
    internal static class SaslFactory
    {
        /// <summary>
        /// A dictionary of Sasl mechanisms registered with the factory class.
        /// </summary>
        private static Dictionary<string, Type> Mechanisms
        {
            get;
            set;
        }

        /// <summary>
        /// Creates an instance of the Sasl mechanism with the specified
        /// name.
        /// </summary>
        /// <param name="name">The name of the Sasl mechanism of which an
        /// instance will be created.</param>
        /// <returns>An instance of the Sasl mechanism with the specified name.</returns>
        /// <exception cref="ArgumentNullException">The name parameter is null.</exception>
        /// <exception cref="SaslException">A Sasl mechanism with the
        /// specified name is not registered with Sasl.SaslFactory.</exception>
        public static SaslMechanism Create(string name)
        {
            name.ThrowIfNull("name");

            if (!Mechanisms.ContainsKey(name))
            {
                throw new SaslException("A Sasl mechanism with the specified name " +
                    "is not registered with Sasl.SaslFactory.");
            }

            
            Type t = Mechanisms[name];

            if (t.Name == "SaslPlain")
            {
                return new SaslPlain();
            }
            else if (t.Name == "SaslDigestMd5")
            {
                return new SaslDigestMd5();
            }
            else if (t.Name == "SaslScramSha1")
            {
                return new SaslScramSha1();
            }

            object o = Activator.CreateInstance(t, true);
            return o as SaslMechanism;
        }

        /// <summary>
        /// Registers a 

        /// <summary>
        /// Static class constructor. Initializes static properties.
        /// </summary>
        static SaslFactory()
        {
            Mechanisms = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);

            // Could be moved to App.config to support SASL "plug-in" mechanisms.
            var list = new Dictionary<string, Type>() {
				{ "PLAIN", typeof(Sasl.Mechanisms.SaslPlain) },
				{ "DIGEST-MD5", typeof(Sasl.Mechanisms.SaslDigestMd5) },
				{ "SCRAM-SHA-1", typeof(Sasl.Mechanisms.SaslScramSha1) },
			};

            foreach (string key in list.Keys)
                Mechanisms.Add(key, list[key]);
        }
    }
}