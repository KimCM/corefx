﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CultureInfo = System.Globalization.CultureInfo;
using Debug = System.Diagnostics.Debug;
using IEnumerable = System.Collections.IEnumerable;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using Enumerable = System.Linq.Enumerable;
using IComparer = System.Collections.IComparer;
using IEqualityComparer = System.Collections.IEqualityComparer;
using StringBuilder = System.Text.StringBuilder;
using Encoding = System.Text.Encoding;
using Interlocked = System.Threading.Interlocked;
using System.Reflection;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents a name of an XML element or attribute. This class cannot be inherited.
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors", Justification = "Deserialization handled by NameSerializer.")]
    public sealed class XName : IEquatable<XName>
    {
        XNamespace ns;
        string localName;
        int hashCode;

        /// <summary>
        /// Constructor, internal so that external users must go through the Get() method to create an XName.
        /// </summary>
        internal XName(XNamespace ns, string localName)
        {
            this.ns = ns;
            this.localName = XmlConvert.VerifyNCName(localName);
            this.hashCode = ns.GetHashCode() ^ localName.GetHashCode();
        }

        /// <summary>
        /// Gets the local (unqualified) part of the name.
        /// </summary>
        /// <seealso cref="XName.Namespace"/>
        public string LocalName
        {
            get { return localName; }
        }

        /// <summary>
        /// Gets the namespace of the name.
        /// </summary>
        public XNamespace Namespace
        {
            get { return ns; }
        }

        /// <summary>
        /// Gets the namespace name part of the name.
        /// </summary>
        public string NamespaceName
        {
            get { return ns.NamespaceName; }
        }

        /// <summary>
        /// Returns the expanded XML name in the format: {namespaceName}localName.
        /// </summary>
        public override string ToString()
        {
            if (ns.NamespaceName.Length == 0) return localName;
            return "{" + ns.NamespaceName + "}" + localName;
        }

        /// <summary>
        /// Returns an <see cref="XName"/> object created from the specified expanded name.
        /// </summary>
        /// <param name="expandedName">
        /// A string containing an expanded XML name in the format: {namespace}localname.
        /// </param>
        /// <returns>
        /// An <see cref="XName"/> object constructed from the specified expanded name.
        /// </returns>
        public static XName Get(string expandedName)
        {
            if (expandedName == null) throw new ArgumentNullException("expandedName");
            if (expandedName.Length == 0) throw new ArgumentException(SR.Format(SR.Argument_InvalidExpandedName, expandedName));
            if (expandedName[0] == '{')
            {
                int i = expandedName.LastIndexOf('}');
                if (i <= 1 || i == expandedName.Length - 1) throw new ArgumentException(SR.Format(SR.Argument_InvalidExpandedName, expandedName));
                return XNamespace.Get(expandedName, 1, i - 1).GetName(expandedName, i + 1, expandedName.Length - i - 1);
            }
            else
            {
                return XNamespace.None.GetName(expandedName);
            }
        }

        /// <summary>
        /// Returns an <see cref="XName"/> object from a local name and a namespace.
        /// </summary>
        /// <param name="localName">A local (unqualified) name.</param>
        /// <param name="namespaceName">An XML namespace.</param>
        /// <returns>An XName object created from the specified local name and namespace.</returns>
        public static XName Get(string localName, string namespaceName)
        {
            return XNamespace.Get(namespaceName).GetName(localName);
        }

        /// <summary>
        /// Converts a string formatted as an expanded XML name ({namespace}localname) to an XName object.
        /// </summary>
        /// <param name="expandedName">A string containing an expanded XML name in the format: {namespace}localname.</param>
        /// <returns>An XName object constructed from the expanded name.</returns>        
        [CLSCompliant(false)]
        public static implicit operator XName(string expandedName)
        {
            return expandedName != null ? Get(expandedName) : null;
        }

        /// <summary>
        /// Determines whether the specified <see cref="XName"/> is equal to the current <see cref="XName"/>.
        /// </summary>
        /// <param name="obj">The XName to compare to the current XName.</param>
        /// <returns>
        /// true if the specified <see cref="XName"/> is equal to the current XName; otherwise false.
        /// </returns>
        /// <remarks>
        /// For two <see cref="XName"/> objects to be equal, they must have the same expanded name.
        /// </remarks>
        public override bool Equals(object obj)
        {
            return (object)this == obj;
        }

        /// <summary>
        /// Serves as a hash function for <see cref="XName"/>. GetHashCode is suitable 
        /// for use in hashing algorithms and data structures like a hash table.  
        /// </summary>
        public override int GetHashCode()
        {
            return hashCode;
        }

        // The overloads of == and != are included to enable comparisons between
        // XName and string (e.g. element.Name == "foo"). C#'s predefined reference
        // equality operators require one operand to be convertible to the type of
        // the other through reference conversions only and do not consider the
        // implicit conversion from string to XName.

        /// <summary>
        /// Returns a value indicating whether two instances of <see cref="XName"/> are equal.
        /// </summary>
        /// <param name="left">The first XName to compare.</param>
        /// <param name="right">The second XName to compare.</param>
        /// <returns>true if left and right are equal; otherwise false.</returns>
        /// <remarks>
        /// This overload is included to enable the comparison between
        /// an instance of XName and string.
        /// </remarks>
        public static bool operator ==(XName left, XName right)
        {
            return (object)left == (object)right;
        }

        /// <summary>
        /// Returns a value indicating whether two instances of <see cref="XName"/> are not equal.
        /// </summary>
        /// <param name="left">The first XName to compare.</param>
        /// <param name="right">The second XName to compare.</param>
        /// <returns>true if left and right are not equal; otherwise false.</returns>
        /// <remarks>
        /// This overload is included to enable the comparison between
        /// an instance of XName and string.
        /// </remarks>
        public static bool operator !=(XName left, XName right)
        {
            return (object)left != (object)right;
        }

        /// <summary>
        /// Indicates whether the current <see cref="XName"/> is equal to 
        /// the specified <see cref="XName"/>
        /// </summary>
        /// <param name="other">The <see cref="XName"/> to compare with the
        /// current <see cref="XName"/></param> 
        /// <returns>
        /// Returns true if the current <see cref="XName"/> is equal to
        /// the specified <see cref="XName"/>. Returns false otherwise. 
        /// </returns>
        bool IEquatable<XName>.Equals(XName other)
        {
            return (object)this == (object)other;
        }
    }
}
