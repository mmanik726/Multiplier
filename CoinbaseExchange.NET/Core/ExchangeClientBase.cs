using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Utilities;

namespace CoinbaseExchange.NET.Core
{
    public abstract class ExchangeClientBase
    {
        public string API_ENDPOINT_URL;
        private const string ContentType = "application/json";

        private bool UsePublicExchange; 

        private readonly CBAuthenticationContainer _authContainer;


        public ExchangeClientBase(CBAuthenticationContainer authContainer, 
            string apiEndPoint = "https://api.gdax.com/")
        {
            API_ENDPOINT_URL = apiEndPoint;
            _authContainer = authContainer;
            UsePublicExchange = false;
        }

        public ExchangeClientBase(string apiEndPoint = "https://api.gdax.com/")
        {
            API_ENDPOINT_URL = apiEndPoint;
            //_authContainer = new CBAuthenticationContainer();
            UsePublicExchange = true;
        }

        protected async Task<ExchangeResponse> GetResponse(ExchangeRequestBase request)
        {
            var relativeUrl = request.RequestUrl;
            var absoluteUri = new Uri(new Uri(API_ENDPOINT_URL), relativeUrl);

            var timeStamp = (request.TimeStamp).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var body = request.RequestBody;
            var method = request.Method;
            var url = absoluteUri.ToString();

            //String passphrase = "";
            //String apiKey = "";
            //// Caution: Use the relative URL, *NOT* the absolute one.
            //var signature = "";


            using (var httpClient = new HttpClient())
            {
                HttpResponseMessage response = null;

                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                try
                {
                    switch (method)
                    {
                        case "GET":
                            try
                            {
                                response = await CoreGetRequest(request);//await httpClient.GetAsync(absoluteUri);
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.ToLower().Contains("task was cancelled"))
                                {
                                    await GetResponse(request);
                                    return null; 
                                } 

                                var innerExMsg = ex.InnerException.Message.ToLower();

                                Logger.WriteLog("Exception occured in GET: " + ex.InnerException.Message);
                                //
                                if (innerExMsg.Contains("could not be resolved") || 
                                    innerExMsg.Contains("unable to connect"))
                                {

                                    while (response == null)
                                    {
                                        Thread.Sleep(5 * 1000);
                                        Logger.WriteLog("Cant connect to server, retryin in 5 sec");

                                        try
                                        {
                                            //SharedTimeStamp = DateTime.UtcNow.ToUnixTimestamp().ToString(System.Globalization.CultureInfo.InvariantCulture);
                                            response = await CoreGetRequest(request); // await httpClient.GetAsync(absoluteUri);
                                        }
                                        catch { };
                                        
                                    }
                                }


                                //throw;
                            }



                            break;
                        case "POST":

                            try
                            {
                                response = await CorePostRequest(request);//await httpClient.GetAsync(absoluteUri);
                            }
                            catch (Exception ex)
                            {
                                var innerExMsg = ex.InnerException.Message.ToLower();

                                Logger.WriteLog("Exception occured in POST: " + ex.InnerException.Message);
                                //
                                if (innerExMsg.Contains("could not be resolved") ||
                                    innerExMsg.Contains("unable to connect"))
                                {

                                    while (response == null)
                                    {
                                        Thread.Sleep(5 * 1000);
                                        Logger.WriteLog("Cant connect to server, retryin in 5 sec");

                                        try
                                        {
                                            //SharedTimeStamp = DateTime.UtcNow.ToUnixTimestamp().ToString(System.Globalization.CultureInfo.InvariantCulture);
                                            response = await CorePostRequest(request); // await httpClient.GetAsync(absoluteUri);
                                        }
                                        catch { };

                                    }
                                }


                                //throw;
                            }
                            break;


                        case "DELETE":
                            //var requestBody = new StringContent(body, Encoding.UTF8, "application/json");
                            try
                            {
                                response = await CoreDeleteRequest(request);//await httpClient.GetAsync(absoluteUri);
                            }
                            catch (Exception ex)
                            {
                                var innerExMsg = ex.InnerException.Message.ToLower();

                                Logger.WriteLog("Exception occured in DELETE: " + ex.InnerException.Message);
                                //
                                if (innerExMsg.Contains("could not be resolved") ||
                                    innerExMsg.Contains("unable to connect"))
                                {

                                    while (response == null)
                                    {
                                        Thread.Sleep(5 * 1000);
                                        Logger.WriteLog("Cant connect to server, retryin in 5 sec");

                                        try
                                        {
                                            //SharedTimeStamp = DateTime.UtcNow.ToUnixTimestamp().ToString(System.Globalization.CultureInfo.InvariantCulture);
                                            response = await CoreDeleteRequest(request); // await httpClient.GetAsync(absoluteUri);
                                        }
                                        catch { };

                                    }
                                }


                                //throw;
                            }
                            break;

                        default:
                            throw new NotImplementedException("The supplied HTTP method is not supported: " + method ?? "(null)");
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error getting/setting server response/data: " + ex.Message);
                    throw new Exception("HTTP_GET_POST_DELTE_Error");
                }

                //Logger.WriteLog("processing rest of http request");

                var contentBody = await response.Content.ReadAsStringAsync();
                var headers = response.Headers.AsEnumerable();
                var statusCode = response.StatusCode;
                var isSuccess = response.IsSuccessStatusCode;

                var genericExchangeResponse = new ExchangeResponse(statusCode, isSuccess, headers, contentBody);
                return genericExchangeResponse;
            }
        }

        private async Task<HttpResponseMessage> CoreGetRequest(ExchangeRequestBase request)
        {

            using (var httpClient = new HttpClient())
            {

                var relativeUrl = request.RequestUrl;
                var absoluteUri = new Uri(new Uri(API_ENDPOINT_URL), relativeUrl);

                var timeStamp = DateTime.UtcNow.ToUnixTimestamp().ToString(System.Globalization.CultureInfo.InvariantCulture); //(request.TimeStamp).ToString(System.Globalization.CultureInfo.InvariantCulture);
                var body = request.RequestBody;
                var method = request.Method;
                var url = absoluteUri.ToString();

                String passphrase = "";
                String apiKey = "";

                var signature = "";

                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                if (UsePublicExchange == false)
                {

                    passphrase = _authContainer.Passphrase;
                    apiKey = _authContainer.ApiKey;

                    signature = _authContainer.ComputeSignature(timeStamp, relativeUrl, method, body);
                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", signature);

                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", apiKey);
                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-TIMESTAMP", timeStamp);
                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", passphrase);

                }

                HttpResponseMessage response = null;
                response = await httpClient.GetAsync(absoluteUri);
                return response;

            }
        }

        private async Task<HttpResponseMessage> CorePostRequest(ExchangeRequestBase request)
        {

            using (var httpClient = new HttpClient())
            {

                var relativeUrl = request.RequestUrl;
                var absoluteUri = new Uri(new Uri(API_ENDPOINT_URL), relativeUrl);

                var timeStamp = DateTime.UtcNow.ToUnixTimestamp().ToString(System.Globalization.CultureInfo.InvariantCulture);
                var body = request.RequestBody;
                var method = request.Method;
                var url = absoluteUri.ToString();

                String passphrase = "";
                String apiKey = "";

                var signature = "";

                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                if (UsePublicExchange == false)
                {

                    passphrase = _authContainer.Passphrase;
                    apiKey = _authContainer.ApiKey;

                    signature = _authContainer.ComputeSignature(timeStamp, relativeUrl, method, body);
                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", signature);

                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", apiKey);
                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-TIMESTAMP", timeStamp);
                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", passphrase);

                }

                //timestamp = (request.TimeStamp).ToString(System.Globalization.CultureInfo.InvariantCulture);
                //signature = _authContainer.ComputeSignature(timestamp, relativeUrl, method, body);
                //httpClient.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", signature);

                HttpResponseMessage response = null;
                var requestBody = new StringContent(body, Encoding.UTF8, "application/json");
                response = await httpClient.PostAsync(absoluteUri, requestBody);

                return response;

            }
        }

        private async Task<HttpResponseMessage> CoreDeleteRequest(ExchangeRequestBase request)
        {

            using (var httpClient = new HttpClient())
            {

                var relativeUrl = request.RequestUrl;
                var absoluteUri = new Uri(new Uri(API_ENDPOINT_URL), relativeUrl);

                var timeStamp = DateTime.UtcNow.ToUnixTimestamp().ToString(System.Globalization.CultureInfo.InvariantCulture); //(request.TimeStamp).ToString(System.Globalization.CultureInfo.InvariantCulture);
                var body = request.RequestBody;
                var method = request.Method;
                var url = absoluteUri.ToString();

                String passphrase = "";
                String apiKey = "";

                var signature = "";

                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                if (UsePublicExchange == false)
                {

                    passphrase = _authContainer.Passphrase;
                    apiKey = _authContainer.ApiKey;

                    signature = _authContainer.ComputeSignature(timeStamp, relativeUrl, method, body);
                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", signature);

                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", apiKey);
                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-TIMESTAMP", timeStamp);
                    httpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", passphrase);

                }

                HttpResponseMessage response = null;
                response = await httpClient.DeleteAsync(absoluteUri);
                return response;

            }
        }

    }
}
