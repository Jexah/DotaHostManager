using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using DotaHostClientLibrary;

namespace DotaHostLibrary
{
    public static class HTTPRequestManager
    {
        public static void startRequest(string url, string method, Action<string> responseAction, Dictionary<string, string> sendData)
        {
            HttpWebRequest request = null;
            url += "?";
            if (method == "GET")
            {
                foreach (KeyValuePair<string, string> kvp in sendData)
                {
                    url += kvp.Key + "=" + kvp.Value + "&";
                }
                if (sendData.Count > 0)
                {
                    url = url.Substring(0, url.Length - 1);
                }
                request = (HttpWebRequest)WebRequest.Create(url);
            }
            if (method == "POST")
            {
                string postData = "";
                foreach (KeyValuePair<string, string> kvp in sendData)
                {
                    if(kvp.Key == "api_key")
                    {
                        url += "api_key=" + kvp.Value;
                    }
                    else
                    {
                        postData += kvp.Key + "=" + kvp.Value + "&";
                    }
                   
                }
                if (sendData.Count > 0)
                {
                    postData = postData.Substring(0, postData.Length - 1);
                }
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.ContentType = "application/x-www-form-urlencoded"; 
                byte[] array = Encoding.ASCII.GetBytes(postData);
                request.ContentLength = array.Length;
                using (var dataStream = request.GetRequestStream())
                {
                    dataStream.Write(array, 0, array.Length);
                    dataStream.Close();
                }
            }
            Action wrapperAction = () =>
            {
                request.BeginGetResponse(new AsyncCallback((iar) =>
                {
                    var response = (HttpWebResponse)((HttpWebRequest)iar.AsyncState).EndGetResponse(iar);
                    var body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    responseAction(body);
                }), request);
            };
            wrapperAction.BeginInvoke(new AsyncCallback((iar) =>
            {
                var action = (Action)iar.AsyncState;
                action.EndInvoke(iar);
            }), wrapperAction);
        }
    }
}
