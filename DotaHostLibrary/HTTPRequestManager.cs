using DotaHostClientLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace DotaHostLibrary
{
    public static class HttpRequestManager
    {
        public static void StartRequest(string url, string method, Action<string, HttpStatusCode> responseAction, dynamic sendData = null, Dictionary<string, string> headers = null)
        {

            ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, ssl) => true;

            // Initialize request object
            HttpWebRequest request = null;

            // Add ? for GET: all params, and POST: just api_key
            url += "?";

            // If method is GET
            if (method == "GET")
            {
                if (sendData != null)
                {
                    // Loop through the kkey value pairs in the data, and append them to the url
                    var getData = new Dictionary<string, string>(sendData);

                    url = getData.Aggregate(url, (current, kvp) => current + (kvp.Key + "=" + kvp.Value + "&"));

                    // Remove the trailing & at the end
                    if (sendData.Count > 0)
                    {
                        url = url.Substring(0, url.Length - 1);
                    }
                }

                // Redefine the request using the new URL
                request = (HttpWebRequest)WebRequest.Create(url);
            }

            // If method is POST
            else if (method == "POST")
            {
                // Define the post data
                string postData = "";

                if (sendData != null)
                {
                    // Loop through the key value pairs in the data, append them to postData, except in the case of api_key, append to URL
                    foreach (var kvp in sendData)
                    {
                        postData += kvp.Key + "=" + kvp.Value + "&";
                    }

                    // Remove trailing &
                    if (sendData.Count > 1)
                    {
                        postData = postData.Substring(0, postData.Length - 1);
                    }
                    Helpers.Log(url);
                }

                // Refine the request using the new URL
                request = (HttpWebRequest)WebRequest.Create(url);

                // Set method to POST
                request.Method = method;

                // Set content type to application/x-www-form-urlencoded
                request.ContentType = "application/x-www-form-urlencoded";

                // Encode the post data into a byte array
                byte[] array = Encoding.ASCII.GetBytes(postData);

                // Define the total length of the stream as the length of the byte array
                request.ContentLength = array.Length;

                // Get the request stream of the request object
                using (var dataStream = request.GetRequestStream())
                {
                    // Write the byte array to the data stream
                    dataStream.Write(array, 0, array.Length);

                    // Close the data stream
                    dataStream.Close();
                }
            }
            else if (method == "POSTJSON")
            {
                // Refine the request using the new URL
                request = (HttpWebRequest)WebRequest.Create(url);

                string json = new JavaScriptSerializer().Serialize(sendData);

                // Set method to POST
                request.Method = "POST";

                // Set content type to application/json
                request.ContentType = "application/json";

                Helpers.Log("'" + json + "'");

                // Get the request stream of the request object
                using (var dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    // Write the byte array to the data stream
                    dataStream.Write(json);

                    dataStream.Flush();

                    // Close the data stream
                    dataStream.Close();
                }
            }

            if (headers != null)
            {
                foreach (var kvp in headers)
                {
                    if (kvp.Key == "Content-Type")
                    {
                        if (request != null) request.ContentType = kvp.Value;
                    }
                    else if (kvp.Key == "Accept")
                    {
                        if (request != null) request.Accept = kvp.Value;
                    }
                    else
                    {
                        if (request != null) request.Headers[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Create the function to get called asynchronously
            Action wrapperAction = () =>
            {
                // Async get response
                if (request != null)
                {
                    request.BeginGetResponse(iar =>
                    {
                        string body;
                        HttpStatusCode responseCode = HttpStatusCode.OK;

                        try
                        {
                            // Get response object
                            var response = (HttpWebResponse)((HttpWebRequest)iar.AsyncState).EndGetResponse(iar);

                            // Read response stream and store data
                            body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        }
                        catch (WebException we)
                        {
                            responseCode = ((HttpWebResponse)we.Response).StatusCode;
                            body = new StreamReader(((HttpWebResponse)we.Response).GetResponseStream()).ReadToEnd();
                        }


                        // Call the given function taking the raw JSON as the parameter
                        responseAction(body, responseCode);


                    }, request);
                }
            };

            // Call of the async function defined above, asynchronously
            wrapperAction.BeginInvoke(iar =>
            {
                var action = (Action)iar.AsyncState;
                action.EndInvoke(iar);
            }, wrapperAction);
        }

    }
}
