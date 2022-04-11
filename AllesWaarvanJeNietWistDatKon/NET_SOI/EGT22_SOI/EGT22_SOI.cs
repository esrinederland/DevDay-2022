// Copyright 2018 ESRI
// 
// All rights reserved under the copyright laws of the United States
// and applicable international laws, treaties, and conventions.
// 
// You may freely redistribute and use this sample code, with or
// without modification, provided you include the original copyright
// notice and use restrictions.
// 
// See the use restrictions at <your Enterprise SDK install location>/userestrictions.txt.
// 

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Server;
using ESRI.Server.SOESupport;
using ESRI.Server.SOESupport.SOI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

//This is SOI template of Enterprise SDK

namespace EGT22_SOI
{
    [ComVisible(true)]
    [Guid("fb6c9cb6-c7d6-4dea-8423-fa9afdb01914")]
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectInterceptor("MapServer",
        Description = "EGT22 SOI v20220405.06",
        DisplayName = "EGT22_SOI",
        Properties = "",
        SupportsSharedInstances = false)]
    public class EGT22_SOI : IServerObjectExtension, IRESTRequestHandler, IWebRequestHandler, IRequestHandler2, IRequestHandler
    {
        private string _soiName;
        private IServerObjectHelper _soHelper;
        private ServerLogger _serverLog;
        private RestSOIHelper _restSOIHelper;

        private string _outputDirectory = string.Empty;

        public EGT22_SOI()
        {
            _soiName = this.GetType().Name;
        }

        public void Init(IServerObjectHelper pSOH)
        {
            System.Diagnostics.Debugger.Launch();
            _soHelper = pSOH;
            _serverLog = new ServerLogger();
            _restSOIHelper = new RestSOIHelper(pSOH);

            try
            {
                IPropertySet configProps = ServerUtilities.QueryConfigurationProperties(pSOH.ServerObject.ConfigurationName, pSOH.ServerObject.TypeName);
                _outputDirectory = configProps.GetProperty("outputDir") as string;
            }
            catch 
            {
                //ignore the exception, just set the dir to empty 
                _outputDirectory = string.Empty;
            }

            _outputDirectory = _outputDirectory.Trim();
            if (string.IsNullOrEmpty(_outputDirectory))
            {
                _serverLog.LogMessage(ServerLogger.msgType.error, _soiName + ".init()", 500, "OutputDirectory is empty or missing. Reset to default.");
                _outputDirectory = "C:\\arcgisserver\\directories\\arcgisoutput";
            }

            _serverLog.LogMessage(ServerLogger.msgType.infoDetailed, _soiName + ".init()", 500, "OutputDirectory is " + _outputDirectory);
            _serverLog.LogMessage(ServerLogger.msgType.infoStandard, _soiName + ".init()", 200, "Initialized " + _soiName + " SOI.");

        }

        public void Shutdown()
        {
            _serverLog.LogMessage(ServerLogger.msgType.infoStandard, _soiName + ".init()", 200, "Shutting down " + _soiName + " SOI.");
        }

        #region REST interceptors

        public string GetSchema()
        {
            IRESTRequestHandler restRequestHandler = _restSOIHelper.FindRequestHandlerDelegate<IRESTRequestHandler>();
            if (restRequestHandler == null)
                return null;

            return restRequestHandler.GetSchema();
        }

        public byte[] HandleRESTRequest_old(string Capabilities, string resourceName, string operationName,
            string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            try
            {
                responseProperties = null;
                _serverLog.LogMessage(ServerLogger.msgType.infoStandard, _soiName + ".HandleRESTRequest()",
                    200, "Request received in Sample Object Interceptor for handleRESTRequest");

                /*
                * Add code to manipulate REST requests here
                */

                // Find the correct delegate to forward the request too
                IRESTRequestHandler restRequestHandler = _restSOIHelper.FindRequestHandlerDelegate<IRESTRequestHandler>();
                if (restRequestHandler == null)
                {
                    throw new RestErrorException("Service handler not found");
                }

                var response = restRequestHandler.HandleRESTRequest(
                        Capabilities, resourceName, operationName, operationInput,
                        outputFormat, requestProperties, out responseProperties);

                return response;
           
            }
            catch (RestErrorException restException)
            {
                responseProperties = "{\"Content-Type\":\"text/plain;charset=utf-8\"}";
                return System.Text.Encoding.UTF8.GetBytes(restException.Message);
            }
            catch (Exception e)
            {
                _serverLog.LogMessage(ServerLogger.msgType.error, _soiName + ".HandleRESTRequest()", 500, "Exception " + e.GetType().Name + " " + e.Message + " " + e.StackTrace);
                throw;
            }
        }



        public byte[] HandleRESTRequest(string Capabilities, string resourceName, string operationName,
            string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            try
            {
                responseProperties = null;
                _serverLog.LogMessage(ServerLogger.msgType.infoStandard, _soiName + ".HandleRESTRequest()",
                    200, "Request received in Sample Object Interceptor for handleRESTRequest");

                /*
                * Add code to manipulate REST requests here
                */

                // Find the correct delegate to forward the request too
                IRESTRequestHandler restRequestHandler = _restSOIHelper.FindRequestHandlerDelegate<IRESTRequestHandler>();
                if (restRequestHandler == null)
                {
                    throw new RestErrorException("Service handler not found");
                }

                var response = restRequestHandler.HandleRESTRequest(
                        Capabilities, resourceName, operationName, operationInput,
                        outputFormat, requestProperties, out responseProperties);

                
                /*
                 * Manipulate the response.
                 * 
                 * Add watermark
                 */

                if (operationName.Equals("export", StringComparison.CurrentCultureIgnoreCase))
                {
                    string waterMarkText = "No Query";
                    var jOperationInput = new JsonObject(operationInput);
                    if (jOperationInput.TryGetJsonObject("layerDefs", out JsonObject jLayerDefs))
                    {
                        waterMarkText = jLayerDefs.ToJson();
                        UnicodeEncoding unicode = new UnicodeEncoding();
                        Byte[] encodedBytes = unicode.GetBytes(waterMarkText);
                        waterMarkText = unicode.GetString(encodedBytes);
                    }
                    Image sourceImage = null;
                    if (outputFormat.Equals("image", StringComparison.CurrentCultureIgnoreCase))
                    {
                        
                        sourceImage = Image.FromStream(new System.IO.MemoryStream(response));

                        var watermarker = new ApplyWatermark();

                        var watermarkedImage = watermarker.Mark(sourceImage, waterMarkText);
                        var newResponse = new System.IO.MemoryStream();
                        watermarkedImage.Save(newResponse, sourceImage.RawFormat);

                        return newResponse.GetBuffer();
                    }
                    else if (outputFormat.Equals("json", StringComparison.CurrentCultureIgnoreCase))
                    {

                        var responseString = System.Text.Encoding.UTF8.GetString(response);
                        var jo = new JsonObject(responseString);
                        string hrefString = null;
                        if (!jo.TryGetString("href", out hrefString))
                            throw new RestErrorException("Export operation returned invalid response");

                        if (string.IsNullOrEmpty(hrefString))
                            throw new RestErrorException("Export operation returned invalid response");

                        // Generate output file location
                        var outputImageFileLocation = GetOutputImageFileLocation(hrefString);

                        // debug logging
                        //_serverLog.LogMessage(ServerLogger.msgType.error, "debug", 0, "output is " + outputImageFileLocation);

                        var watermarker = new ApplyWatermark();
                        Image watermarkedImage;

                        System.Drawing.Imaging.ImageFormat sourceImageFormat;
                        using (sourceImage = Image.FromFile(outputImageFileLocation))
                        {
                            sourceImageFormat = sourceImage.RawFormat;
                            watermarkedImage = watermarker.Mark(sourceImage, waterMarkText);
                        }
                        // make sure we dispose sourceImage handles before saving to it

                        watermarkedImage.Save(outputImageFileLocation, sourceImageFormat);
                        watermarkedImage.Dispose();

                        // return response as is because we have modified the file its pointing to
                        return response;
                    }
                    else if (outputFormat.Equals("kmz", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Note: Watermark can be added for the kmz format too. In this example we didn't
                        // implement it.
                        throw new RestErrorException("Kmz format is not supported");
                    }
                    else
                    {
                        throw new RestErrorException("Invalid operation parameters");
                    }
                }//if operationName==export
                return response;
            }
            catch (RestErrorException restException)
            {
                responseProperties = "{\"Content-Type\":\"text/plain;charset=utf-8\"}";
                return System.Text.Encoding.UTF8.GetBytes(restException.Message);
            }
            catch (Exception e)
            {
                _serverLog.LogMessage(ServerLogger.msgType.error, _soiName + ".HandleRESTRequest()", 500, "Exception " + e.GetType().Name + " " + e.Message + " " + e.StackTrace);
                throw;
            }
        }

        #endregion

        #region SOAP interceptors

        public byte[] HandleStringWebRequest(esriHttpMethod httpMethod, string requestURL,
            string queryString, string Capabilities, string requestData,
            out string responseContentType, out esriWebResponseDataType respDataType)
        {
            _serverLog.LogMessage(ServerLogger.msgType.infoStandard, _soiName + ".HandleStringWebRequest()",
                200, "Request received in Sample Object Interceptor for HandleStringWebRequest");

            /*
             * Add code to manipulate requests here
             */

            IWebRequestHandler webRequestHandler = _restSOIHelper.FindRequestHandlerDelegate<IWebRequestHandler>();
            if (webRequestHandler != null)
            {
                return webRequestHandler.HandleStringWebRequest(
                        httpMethod, requestURL, queryString, Capabilities, requestData, out responseContentType, out respDataType);
            }

            responseContentType = null;
            respDataType = esriWebResponseDataType.esriWRDTPayload;
            //Insert error response here.
            return null;
        }

        public byte[] HandleBinaryRequest(ref byte[] request)
        {
            _serverLog.LogMessage(ServerLogger.msgType.infoStandard, _soiName + ".HandleBinaryRequest()",
                  200, "Request received in Sample Object Interceptor for HandleBinaryRequest");

            /*
             * Add code to manipulate requests here
             */

            IRequestHandler requestHandler = _restSOIHelper.FindRequestHandlerDelegate<IRequestHandler>();
            if (requestHandler != null)
            {
                return requestHandler.HandleBinaryRequest(request);
            }

            //Insert error response here.
            return null;
        }

        public byte[] HandleBinaryRequest2(string Capabilities, ref byte[] request)
        {
            _serverLog.LogMessage(ServerLogger.msgType.infoStandard, _soiName + ".HandleBinaryRequest2()",
                  200, "Request received in Sample Object Interceptor for HandleBinaryRequest2");

            /*
             * Add code to manipulate requests here
             */

            IRequestHandler2 requestHandler = _restSOIHelper.FindRequestHandlerDelegate<IRequestHandler2>();
            if (requestHandler != null)
            {
                return requestHandler.HandleBinaryRequest2(Capabilities, request);
            }

            //Insert error response here.
            return null;
        }

        public string HandleStringRequest(string Capabilities, string request)
        {
            _serverLog.LogMessage(ServerLogger.msgType.infoStandard, _soiName + ".HandleStringRequest()",
                   200, "Request received in Sample Object Interceptor for HandleStringRequest");

            /*
             * Add code to manipulate requests here
             */

            IRequestHandler requestHandler = _restSOIHelper.FindRequestHandlerDelegate<IRequestHandler>();
            if (requestHandler != null)
            {
                return requestHandler.HandleStringRequest(Capabilities, request);
            }

            //Insert error response here.
            return null;
        }

        #endregion


        #region Utility code


        /**
        * Generate physical file path from virtual path
        * 
        * @param virtualPath Path returned by the MapServer SO
        * @return
        */
        private String GetOutputImageFileLocation(String virtualPath)
        {
            /*
             * Sample output returned by MapServer SO
             * 
             * example : /rest/directories/arcgisoutput/SampleWorldCities_MapServer/
             * _ags_map26c62f8c2c0c4965b53e87e300e1912f.png
             */
            var virtualPathParts = virtualPath.Split('/');
            String imageFileLocation = _outputDirectory;

            // build the physical path to the image file
            bool buildPath = false;
            foreach (String virtualPathPart in virtualPathParts)
            {
                if (buildPath)
                {
                    imageFileLocation += "\\" + virtualPathPart;
                }
                if (virtualPathPart.Equals("arcgisoutput", StringComparison.CurrentCultureIgnoreCase))
                {
                    buildPath = true;
                }
            }

            return imageFileLocation;
        }
        #endregion
    }
}
