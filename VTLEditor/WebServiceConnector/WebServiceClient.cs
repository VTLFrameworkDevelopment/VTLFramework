﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebServiceClient.cs" company="Eurostat">
//   Date Created : 2011-09-29
//   Copyright (c) 2011 by the European   Commission, represented by Eurostat. All rights reserved.
//   Licensed under the European Union Public License (EUPL) version 1.1. 
//   If you do not accept this license, you are not allowed to make any use of this file.
// </copyright>
// <summary>
//   This class implements a NSI Web Service client
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebServiceConnector
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Web.Services.Description;
    using System.Xml;
    using System.Xml.Schema;

    using WebServiceConnector.Properties;

    /// <summary>
    /// This class implements a NSI Web Service client
    /// </summary>
    public class WebServiceClient
    {
        #region Constants and Fields

        /// <summary>
        /// SOAP 1.1 namespace
        /// </summary>
        private const string Soap11Ns = "http://schemas.xmlsoap.org/soap/envelope/";

        /// <summary>
        /// SOAP 1.2 namespace
        /// </summary>
        private const string Soap12Ns = "http://www.w3.org/2003/05/soap-envelope";

        /// <summary>
        /// This field holds a template for soap 1.1 request envelope
        /// </summary>
        private const string SoapRequest =
            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:{0}=\"{1}\"><soap:Header/><soap:Body></soap:Body></soap:Envelope>";

        /// <summary>
        /// The SOAP Action HTTP Header
        /// </summary>
        private const string SoapActionHttpHeader = "SOAPAction";

        /// <summary>
        /// The HTTP HEADER Content type value
        /// </summary>
        private const string ContentTypeValue = "text/xml;charset=\"utf-8\"";

        /// <summary>
        /// This field holds the list of the Web Service available operations
        /// as defined in the web service WSDL
        /// </summary>
        private readonly List<string> _availableOperations;

        /// <summary>
        /// This field holds the a map between a web service operation name
        /// and the parameter wrapper element name. This is used for connecting to .NET WS.
        /// </summary>
        private readonly Dictionary<string, string> _operationParameterName;

        /// <summary>
        /// This field holds the Web Service settings
        /// </summary>
        private readonly WsConfigurationSettings _settings;

        /// <summary>
        /// This field holds the web request
        /// </summary>
        private readonly HttpWebRequest _webRequest;

        /// <summary>
        /// The target namespace from the WSDL
        /// </summary>
        private string _namespace;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServiceClient"/> class. 
        /// Initialize a new instance of the WebServiceClient class
        /// It sets the endpoint, WSDL URL, authentication, proxy and method information
        /// </summary>
        /// <param name="settings">
        /// The Web Service settings.
        /// </param>
        public WebServiceClient(WsConfigurationSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            this._settings = settings;
            this._availableOperations = new List<string>();
            this._operationParameterName = new Dictionary<string, string>();
   
            try
            {
                this.ParseWSDL();
            }
            catch (WebException ex)
            {
                var errorMessage = this.HandleWebException(ex);
                //throw new WebException(string.Format(enCulture, Resources.ErrorParsingWsdlFormat1, errorMessage), ex, ex.Status, ex.Response);
                throw new WebException(errorMessage, ex, ex.Status, ex.Response);
            }
            catch (InvalidOperationException ex)
            {
                //throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.ErrorParsingWsdlFormat1, ex.Message, ex));
                throw new InvalidOperationException( ex.Message);
            }
            catch (UriFormatException ex)
            {
                //throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.ErrorParsingWsdlFormat1, ex.Message, ex));
                throw new InvalidOperationException(ex.Message);
            }

            try
            {
                this._webRequest = this.CreateWebRequest();
            }
            catch (WebException ex)
            {
                var errorMessage = this.HandleWebException(ex);
                throw new WebException("Error accessing Web Service. " + errorMessage, ex);
            }
            catch (UriFormatException ex)
            {
                throw new InvalidOperationException("Error parsing Endpoint URL." + ex.Message, ex);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the WebRequest used by this object
        /// </summary>
        public HttpWebRequest WsWebRequest
        {
            get
            {
                return this._webRequest;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Invoke the web method with the given Soap Envelope or SDMX-ML Query
        /// </summary>
        /// <param name="soapRequest">
        /// The soap Request.
        /// </param>
        /// <returns>
        /// The Web Service response
        /// </returns>
        public WebResponse InvokeMethod(XmlDocument soapRequest)
        {
            if (soapRequest == null)
            {
                throw new ArgumentNullException("soapRequest");
            }

            //XmlDocument doc;
            if (!IsSoapMessage(soapRequest))
            {
                soapRequest = this.CreateSoapRequestEnvelope(soapRequest);
            }

            using (Stream stream = this.WsWebRequest.GetRequestStream())
            {
                soapRequest.Save(stream);
            }

            try
            {

                return this.WsWebRequest.GetResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    /*using (Stream stream = ex.Response.GetResponseStream())
                    {
                        doc = new XmlDocument();
                        if (stream != null)
                        {
                            StreamReader sr = new StreamReader(stream, Encoding.UTF8, true);
                            char[] buffer = new char[nBytes + 1];
                            int nBytesRead = sr.Read(buffer, 0, nBytes + 1);
                            if (nBytesRead <= nBytes)
                            {
                                doc.Load(stream);
                            }
                            else
                            {
                                this._truncatedXml = new string(buffer, 0, nBytes);
                            }
                        }
                    }*/
                    return ex.Response;
                }
                else
                {
                    throw;
                }
            }

            return null;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check if the given XmlDocument is a Soap Envelope
        /// </summary>
        /// <param name="doc">
        /// The XML Document to check
        /// </param>
        /// <returns>
        /// True if the Xml Document is a SOAP Envelope, false otherwise
        /// </returns>
        private static bool IsSoapMessage(XmlDocument doc)
        {
            bool ret = false;

            XmlElement root = doc.DocumentElement;
            if (root != null)
            {
                ret = root.LocalName.Equals("Envelope")
                      && (root.NamespaceURI.Equals(Soap11Ns) || root.NamespaceURI.Equals(Soap12Ns));
            }

            return ret;
        }

        /// <summary>
        /// Create a web request to the endpoint from the <see cref="_settings"/> values
        /// </summary>
        /// <returns>
        /// The HttpWebRequest object for the configured endpoint
        /// </returns>
        private WebRequest CreateRequestWsdl()
        {
            WebRequest webRequest = /*(HttpWebRequest)*/ WebRequest.Create(this._settings.WSDL);

            // webRequest.Headers.Add("SOAPAction", String.Format("{0}/{1}",_namespace,_settings.Operation));
            // webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            // webRequest.Accept = "text/xml";
            webRequest.Method = "GET";
            if (this._settings.EnableHTTPAuthenication)
            {
                webRequest.Credentials = new NetworkCredential(
                    this._settings.Username, this._settings.Password, this._settings.Domain);
            }

            if (this._settings.EnableProxy)
            {
                if (this._settings.UseSystemProxy)
                {
                    webRequest.Proxy = WebRequest.DefaultWebProxy;
                }
                else
                {
                    var proxy = new WebProxy(this._settings.ProxyServer, this._settings.ProxyServerPort);
                    if (!string.IsNullOrEmpty(this._settings.ProxyUsername)
                        || !string.IsNullOrEmpty(this._settings.ProxyPassword))
                    {
                        proxy.Credentials = new NetworkCredential(
                            this._settings.ProxyUsername, this._settings.ProxyPassword);
                    }

                    webRequest.Proxy = proxy;
                }
            }

            return webRequest;
        }

        /// <summary>
        /// Encapsulate the give SDMX-ML Query in a SOAP Envelope request
        /// </summary>
        /// <param name="sdmxQuery">
        /// The SDMX-ML Query as a XML Document
        /// </param>
        /// <returns>
        /// The Soap Envelope containing the SDMX-ML Query
        /// </returns>
        private XmlDocument CreateSoapRequestEnvelope(XmlDocument sdmxQuery)
        {
            if (sdmxQuery == null)
            {
                throw new ArgumentNullException("sdmxQuery");
            }

            var sb = new StringBuilder();
            sb.AppendFormat(SoapRequest, this._settings.Prefix, this._namespace);
            var doc = new XmlDocument();
            doc.LoadXml(sb.ToString());
            XmlNodeList nodes = doc.GetElementsByTagName("Body", Soap11Ns);
            XmlElement operation = doc.CreateElement(this._settings.Prefix, this._settings.Operation, this._namespace);
            string parameterName;
            XmlElement queryParent = operation;
            if (this._operationParameterName.TryGetValue(this._settings.Operation, out parameterName)
                && !string.IsNullOrEmpty(parameterName))
            {
                queryParent = doc.CreateElement(this._settings.Prefix, parameterName, this._namespace);
                operation.AppendChild(queryParent);
            }

            if (sdmxQuery.DocumentElement != null)
            {
                XmlNode sdmxQueryNode = doc.ImportNode(sdmxQuery.DocumentElement, true);
                queryParent.AppendChild(sdmxQueryNode);
            }

            nodes[0].AppendChild(operation);
            return doc;
        }

        /// <summary>
        /// Create a web request to the endpoint from the <see cref="_settings"/> values
        /// </summary>
        /// <returns>
        /// The HttpWebRequest object for the configured endpoint
        /// </returns>
        private HttpWebRequest CreateWebRequest()
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(this._settings.EndPoint);
            var soapAction = string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}",
                this._namespace,
                this._namespace.EndsWith("/", StringComparison.Ordinal) ? string.Empty : "/",
                this._settings.Operation);
            webRequest.Headers.Add(SoapActionHttpHeader, soapAction);
            webRequest.ContentType = ContentTypeValue;

            // webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            if (this._settings.EnableHTTPAuthenication)
            {
                webRequest.Credentials = new NetworkCredential(
                    this._settings.Username, this._settings.Password, this._settings.Domain);
            }

            if (this._settings.EnableProxy)
            {
                if (this._settings.UseSystemProxy)
                {
                    webRequest.Proxy = WebRequest.DefaultWebProxy;
                }
                else
                {
                    var proxy = new WebProxy(this._settings.ProxyServer, this._settings.ProxyServerPort);
                    if (!string.IsNullOrEmpty(this._settings.ProxyUsername)
                        || !string.IsNullOrEmpty(this._settings.ProxyPassword))
                    {
                        proxy.Credentials = new NetworkCredential(
                            this._settings.ProxyUsername, this._settings.ProxyPassword);
                    }

                    webRequest.Proxy = proxy;
                }
            }

            return webRequest;
        }

        /// <summary>
        /// Get a more user friendly error message from <paramref name="webException"/> 
        /// </summary>
        /// <param name="webException">
        /// The <see cref="WebException"/>
        /// </param>
        /// <returns>
        /// The a more user friendly error message from <paramref name="webException"/>
        /// </returns>
        private string HandleWebException(WebException webException)
        {
            var errorResponse = webException.Response as HttpWebResponse;
            string errorMessage = webException.Message;
            if (errorResponse != null)
            {
                switch (errorResponse.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        errorMessage = this._settings.EnableHTTPAuthenication
                                           ? Resources.error_401_invalid_cred
                                           : Resources.error_401_enableAuthentication;
                        break;
                    default:
                        errorMessage = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.error_http,
                            (int)errorResponse.StatusCode,
                            errorResponse.StatusDescription,
                            webException.Message);
                        break;
                }
            }

            return errorMessage;
        }

        /// <summary>
        /// Parse the WSDL and populate the <see cref="_availableOperations"/> and
        /// <see cref="_operationParameterName"/>
        /// </summary>
        private void ParseWSDL()
        {
            WebRequest request = this.CreateRequestWsdl();
            using (WebResponse response = request.GetResponse())
            {
                Stream stream = response.GetResponseStream();
                if (stream != null)
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        ServiceDescription wsdl = ServiceDescription.Read(streamReader);
                        this._namespace = wsdl.TargetNamespace;
                        foreach (PortType pt in wsdl.PortTypes)
                        {
                            foreach (Operation op in pt.Operations)
                            {
                                this._availableOperations.Add(op.Name);
                            }
                        }

                        // Tested with .net and java NSI WS WSDL
                        foreach (XmlSchema schema in wsdl.Types.Schemas)
                        {
                            foreach (XmlSchemaElement element in schema.Elements.Values)
                            {
                                if (element.RefName.IsEmpty)
                                {
                                    var complexType = element.SchemaType as XmlSchemaComplexType;
                                    if (complexType != null)
                                    {
                                        var seq = complexType.Particle as XmlSchemaSequence;
                                        if (seq != null && seq.Items.Count == 1)
                                        {
                                            var body = seq.Items[0] as XmlSchemaElement;
                                            if (body != null && body.RefName.IsEmpty)
                                            {
                                                this._operationParameterName.Add(element.Name, body.Name);
                                            }
                                        }
                                    }
                                }
                            }                            
                        }
                        streamReader.Close();
                    }
                }
                response.Close();
            }
            request.GetResponse().Close();
            request = null;
        }

        #endregion
    }
}