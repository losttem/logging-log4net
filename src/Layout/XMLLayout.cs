#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Text;
using System.Xml;
using System.IO;

using log4net.Core;
using log4net.Util;

namespace log4net.Layout
{
	/// <summary>
	/// Layout that formats the log events as XML elements.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The output of the <see cref="XmlLayout" /> consists of a series of 
	/// log4net:event elements. It does not output a complete well-formed XML 
	/// file. The output is designed to be included as an <em>external entity</em>
	/// in a separate file to form a correct XML file.
	/// </para>
	/// <para>
	/// For example, if <c>abc</c> is the name of the file where
	/// the <see cref="XmlLayout" /> output goes, then a well-formed XML file would 
	/// be:
	/// </para>
	/// <code>
	/// &lt;?xml version="1.0" ?&gt;
	/// 
	/// &lt;!DOCTYPE log4net:events SYSTEM "log4net-events.dtd" [&lt;!ENTITY data SYSTEM "abc"&gt;]&gt;
	///
	/// &lt;log4net:events version="1.2" xmlns:log4net="http://log4net.sourceforge.net/"&gt;
	///     &amp;data;
	/// &lt;/log4net:events&gt;
	/// </code>
	/// <para>
	/// This approach enforces the independence of the <see cref="XmlLayout" /> 
	/// and the appender where it is embedded.
	/// </para>
	/// <para>
	/// The <c>version</c> attribute helps components to correctly
	/// interpret output generated by <see cref="XmlLayout" />. The value of 
	/// this attribute should be "1.2" for release 1.2 and later.
	/// </para>
	/// <para>
	/// Alternatively the <c>Header</c> and <c>Footer</c> properties can be
	/// configured to output the correct XML header, open tag and close tag.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class XmlLayout : XmlLayoutBase
	{
		#region Public Instance Constructors

		/// <summary>
		/// Constructs an XmlLayout
		/// </summary>
		public XmlLayout() : base()
		{
		}

		/// <summary>
		/// Constructs an XmlLayout.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The <b>LocationInfo</b> option takes a boolean value. By
		/// default, it is set to false which means there will be no location
		/// information output by this layout. If the the option is set to
		/// true, then the file name and line number of the statement
		/// at the origin of the log statement will be output. 
		/// </para>
		/// <para>
		/// If you are embedding this layout within an SMTPAppender
		/// then make sure to set the <b>LocationInfo</b> option of that 
		/// appender as well.
		/// </para>
		/// </remarks>
		public XmlLayout(bool locationInfo) :  base(locationInfo)
		{
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// The prefix to use for all element names
		/// </summary>
		/// <remarks>
		/// <para>
		/// The default prefix is <b>log4net</b>. Set this property
		/// to change the prefix. If the prefix is set to an empty string
		/// then no prefix will be written.
		/// </para>
		/// </remarks>
		public string Prefix
		{
			get { return m_prefix; }
			set { m_prefix = value; }
		}

		#endregion Public Instance Properties

		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialize layout options
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is part of the <see cref="IOptionHandler"/> delayed object
		/// activation scheme. The <see cref="ActivateOptions"/> method must 
		/// be called on this object after the configuration properties have
		/// been set. Until <see cref="ActivateOptions"/> is called this
		/// object is in an undefined state and must not be used. 
		/// </para>
		/// <para>
		/// If any of the configuration properties are modified then 
		/// <see cref="ActivateOptions"/> must be called again.
		/// </para>
		/// <para>
		/// Builds a cache of the element names
		/// </para>
		/// </remarks>
		override public void ActivateOptions() 
		{
			base.ActivateOptions();

			// Cache the full element names including the prefix
			if (m_prefix != null && m_prefix.Length > 0)
			{
				m_elmEvent = m_prefix + ":" + ELM_EVENT;
				m_elmMessage = m_prefix + ":" + ELM_MESSAGE;
				m_elmNdc = m_prefix + ":" + ELM_NDC;
				m_elmMdc = m_prefix + ":" + ELM_MDC;
				m_elmProperties = m_prefix + ":" + ELM_PROPERTIES;
				m_elmData = m_prefix + ":" + ELM_DATA;
				m_elmException = m_prefix + ":" + ELM_EXCEPTION;
				m_elmLocation = m_prefix + ":" + ELM_LOCATION;
			}
		}

		#endregion Implementation of IOptionHandler

		#region Override implementation of XMLLayoutBase

		/// <summary>
		/// Does the actual writing of the XML.
		/// </summary>
		/// <param name="writer">The writer to use to output the event to.</param>
		/// <param name="loggingEvent">The event to write.</param>
		override protected void FormatXml(XmlWriter writer, LoggingEvent loggingEvent)
		{
			writer.WriteStartElement(m_elmEvent);
			writer.WriteAttributeString(ATTR_LOGGER, loggingEvent.LoggerName);
			writer.WriteAttributeString(ATTR_TIMESTAMP, XmlConvert.ToString(loggingEvent.TimeStamp));
			writer.WriteAttributeString(ATTR_LEVEL, loggingEvent.Level.ToString());
			writer.WriteAttributeString(ATTR_THREAD, loggingEvent.ThreadName);

			if (loggingEvent.Domain != null && loggingEvent.Domain.Length > 0)
			{
				writer.WriteAttributeString(ATTR_DOMAIN, loggingEvent.Domain);
			}
			if (loggingEvent.Identity != null && loggingEvent.Identity.Length > 0)
			{
				writer.WriteAttributeString(ATTR_IDENTITY, loggingEvent.Identity);
			}
			if (loggingEvent.UserName != null && loggingEvent.UserName.Length > 0)
			{
				writer.WriteAttributeString(ATTR_USERNAME, loggingEvent.UserName);
			}
    
			// Append the message text
			writer.WriteStartElement(m_elmMessage);
			Transform.WriteEscapedXmlString(writer, loggingEvent.RenderedMessage);
			writer.WriteEndElement();

			if (loggingEvent.NestedContext != null && loggingEvent.NestedContext.Length > 0)
			{
				// Append the NDC text
				writer.WriteStartElement(m_elmNdc);
				Transform.WriteEscapedXmlString(writer, loggingEvent.NestedContext);
				writer.WriteEndElement();
			}

			if (loggingEvent.MappedContext != null && loggingEvent.MappedContext.Count > 0)
			{
				// Append the MDC text
				writer.WriteStartElement(m_elmMdc);
				foreach(System.Collections.DictionaryEntry entry in loggingEvent.MappedContext)
				{
					writer.WriteStartElement(m_elmData);
					writer.WriteAttributeString(ATTR_NAME, entry.Key.ToString());
					writer.WriteAttributeString(ATTR_VALUE, entry.Value.ToString());
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
			}

			if (loggingEvent.Properties != null)
			{
				// Append the properties text
				string[] propKeys = loggingEvent.Properties.GetKeys();
				if (propKeys.Length > 0)
				{
					writer.WriteStartElement(m_elmProperties);
					foreach(string key in propKeys)
					{
						writer.WriteStartElement(m_elmData);
						writer.WriteAttributeString(ATTR_NAME, key);
						writer.WriteAttributeString(ATTR_VALUE, loggingEvent.Properties[key].ToString());
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
			}

			string exceptionStr = loggingEvent.GetExceptionString();
			if (exceptionStr != null && exceptionStr.Length > 0)
			{
				// Append the stack trace line
				writer.WriteStartElement(m_elmException);
				Transform.WriteEscapedXmlString(writer, exceptionStr);
				writer.WriteEndElement();
			}

			if (LocationInfo)
			{ 
				LocationInfo locationInfo = loggingEvent.LocationInformation;

				writer.WriteStartElement(m_elmLocation);
				writer.WriteAttributeString(ATTR_CLASS, locationInfo.ClassName);
				writer.WriteAttributeString(ATTR_METHOD, locationInfo.MethodName);
				writer.WriteAttributeString(ATTR_FILE, locationInfo.FileName);
				writer.WriteAttributeString(ATTR_LINE, locationInfo.LineNumber);
				writer.WriteEndElement();
			}

			writer.WriteEndElement();
		}

		#endregion Override implementation of XMLLayoutBase

		#region Private Instance Fields
  
		/// <summary>
		/// The prefix to use for all generated element names
		/// </summary>
		private string m_prefix = PREFIX;

		private string m_elmEvent = ELM_EVENT;
		private string m_elmMessage = ELM_MESSAGE;
		private string m_elmNdc = ELM_NDC;
		private string m_elmMdc = ELM_MDC;
		private string m_elmData = ELM_DATA;
		private string m_elmProperties = ELM_PROPERTIES;
		private string m_elmException = ELM_EXCEPTION;
		private string m_elmLocation = ELM_LOCATION;

		#endregion Private Instance Fields

		#region Private Static Fields

		private const string PREFIX = "log4net";

		private const string ELM_EVENT = "event";
		private const string ELM_MESSAGE = "message";
		private const string ELM_NDC = "ndc";
		private const string ELM_MDC = "mdc";
		private const string ELM_PROPERTIES = "properties";
		private const string ELM_DATA = "data";
		private const string ELM_EXCEPTION = "exception";
		private const string ELM_LOCATION = "locationInfo";

		private const string ATTR_LOGGER = "logger";
		private const string ATTR_TIMESTAMP = "timestamp";
		private const string ATTR_LEVEL = "level";
		private const string ATTR_THREAD = "thread";
		private const string ATTR_DOMAIN = "domain";
		private const string ATTR_IDENTITY = "identity";
		private const string ATTR_USERNAME = "username";
		private const string ATTR_CLASS = "class";
		private const string ATTR_METHOD = "method";
		private const string ATTR_FILE = "file";
		private const string ATTR_LINE = "line";
		private const string ATTR_NAME = "name";
		private const string ATTR_VALUE = "value";

		#endregion Private Static Fields
	}
}

