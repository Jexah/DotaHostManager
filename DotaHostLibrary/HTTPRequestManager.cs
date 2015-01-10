using DotaHostClientLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace DotaHostLibrary
{
    public static class HTTPRequestManager
    {
        public static void startRequest(string url, string method, Action<string> responseAction, Dictionary<string, string> sendData = null, Dictionary<string, string> headers = null)
        {
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
                    foreach (KeyValuePair<string, string> kvp in sendData)
                    {
                        url += kvp.Key + "=" + kvp.Value + "&";
                    }

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
            if (method == "POST")
            {
                // Define the post data
                string postData = "";

                if (sendData != null)
                {
                    // Loop through the key value pairs in the data, append them to postData, except in the case of api_key, append to URL
                    foreach (KeyValuePair<string, string> kvp in sendData)
                    {
                        if (kvp.Key == "api_key")
                        {
                            url += "api_key=" + kvp.Value;
                        }
                        else
                        {
                            postData += kvp.Key + "=" + kvp.Value + "&";
                        }
                    }

                    // Remove trailing &
                    if (sendData.Count > 1)
                    {
                        postData = postData.Substring(0, postData.Length - 1);
                    }
                    Helpers.log(url);
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

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    if (kvp.Key == "Content-Type")
                    {
                        request.ContentType = kvp.Value;
                    }
                    else
                    {
                        request.Headers[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Create the function to get called asynchronously
            Action wrapperAction = () =>
            {
                // Async get response
                request.BeginGetResponse(new AsyncCallback((iar) =>
                {
                    // Get response object
                    var response = (HttpWebResponse)((HttpWebRequest)iar.AsyncState).EndGetResponse(iar);

                    // Read response stream and store data
                    var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    /* NameValueCollection headers2 = response.Headers;
                     for (int i = 0; i < headers.Count; i++)
                     {
                         string key = headers2.GetKey(i);
                         string value = headers2.Get(i);
                         Console.WriteLine(key + " = " + value);
                     }*/

                    // Call the given function taking the raw JSON as the parameter
                    responseAction(body);

                }), request);
            };

            // Call of the async function defined above, asynchronously
            wrapperAction.BeginInvoke(new AsyncCallback((iar) =>
            {
                var action = (Action)iar.AsyncState;
                action.EndInvoke(iar);
            }), wrapperAction);
        }

    }
}
