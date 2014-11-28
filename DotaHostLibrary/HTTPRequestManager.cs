using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace DotaHostLibrary
{
    public static class HTTPRequestManager
    {
        public static void startRequest(string url, string method, Action<dynamic> responseAction, Dictionary<string, string> sendData)
        {
            HttpWebRequest request =(HttpWebRequest)WebRequest.Create(url);
            if (method == "POST")
            {
                NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
                foreach (KeyValuePair<string, string> kvp in sendData)
                {
                    outgoingQueryString.Add(kvp.Key, kvp.Value);
                }
                string postData = outgoingQueryString.ToString();
                request.ContentLength = postData.Length;
                byte[] array = Encoding.ASCII.GetBytes(postData);
                using (var dataStream = request.GetRequestStream())
                {
                    dataStream.Write(array, 0, postData.Length);
                }
            }
            else if (method == "GET")
            {
                url += "?";
                foreach (KeyValuePair<string, string> kvp in sendData)
                {
                    url += kvp.Key + "=" + kvp.Value + "&";
                }
                if (sendData.Count > 0)
                {
                    url = url.Substring(0, url.Length - 1);
                }
            }
            request.Method = method;
            Action wrapperAction = () =>
            {
                request.BeginGetResponse(new AsyncCallback((iar) =>
                {
                    var response = (HttpWebResponse)((HttpWebRequest)iar.AsyncState).EndGetResponse(iar);
                    var body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    dynamic data = JsonConvert.DeserializeObject(body);
                    responseAction(data);
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
