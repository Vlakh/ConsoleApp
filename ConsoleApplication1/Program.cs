using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Diagnostics;
using System.IO;



namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            const String strClient = "https://developer.api.autodesk.com";
            RestClient _client = new RestClient(strClient);

            const String strConsumerKey = "tjmflzuPtJv1AAUcnsLPbGVGXD9PXAcy";
            const String strConsumerSecret = "pTl0gzqTdkrOEOd6";

            String _token = "";

            RestRequest authReq = new RestRequest();
            authReq.Resource = "authentication/v1/authenticate";
            authReq.Method = Method.POST;
            authReq.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            authReq.AddParameter("client_id", strConsumerKey);
            authReq.AddParameter("client_secret", strConsumerSecret);
            authReq.AddParameter("grant_type", "client_credentials");
            IRestResponse result = _client.Execute(authReq);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                String responseString = result.Content;
                int len = responseString.Length;
                int index = responseString.IndexOf("\"access_token\":\"") + "\"access_token\":\"".Length;
                responseString = responseString.Substring(index, len - index - 1);
                int index2 = responseString.IndexOf("\"");
                _token = responseString.Substring(0, index2);
                Console.WriteLine("Token : " + _token);
                //now set the token.
                RestRequest setTokenReq = new RestRequest();
                setTokenReq.Resource = "utility/v1/settoken";
                setTokenReq.Method = Method.POST;
                setTokenReq.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                setTokenReq.AddParameter("access-token", _token);

                IRestResponse resp = _client.Execute(setTokenReq);
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //done...
                    Console.WriteLine("Set token Successfully");
                }
            }

            string bucketname = "vvv-mehanism";
            RestRequest bucketReq = new RestRequest();
            bucketReq.Resource = "oss/v1/buckets";
            bucketReq.Method = Method.POST;
            bucketReq.AddParameter("Authorization", "Bearer " + _token, ParameterType.HttpHeader);
            bucketReq.AddParameter("Content-Type", "application/json", ParameterType.HttpHeader);
            string body = "{\"bucketKey\":\"" + bucketname + "\",\"servicesAllowed\":{},\"policy\":\"transient\"}";
            bucketReq.AddParameter("application/json", body, ParameterType.RequestBody);

            IRestResponse resp2 = _client.Execute(bucketReq);
            if (resp2.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                Console.WriteLine("Bucket " + bucketname + " already present");
            }
            if (resp2.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("Bucket " + bucketname + " created");
            }

            string strFile;
            strFile = Console.ReadLine();
      
            string fileUrn="";
            RestRequest uploadReq = new RestRequest();

            string strFilename = System.IO.Path.GetFileName(strFile);
            string objectKey = HttpUtility.UrlEncode(strFilename);

            FileStream file = File.Open(strFile, FileMode.Open);
            byte[] fileData = null;
            int nlength = (int)file.Length;
            using (BinaryReader reader = new BinaryReader(file))
            {
                fileData = reader.ReadBytes(nlength);
            }
            uploadReq.Resource = "oss/v1/buckets/" + bucketname.ToLower() + "/objects/" + objectKey;
            uploadReq.Method = Method.PUT;
            uploadReq.AddParameter("Authorization", "Bearer " + _token, ParameterType.HttpHeader);
            uploadReq.AddParameter("Content-Type", "application/stream");
            uploadReq.AddParameter("Content-Length", nlength);
            uploadReq.AddParameter("requestBody", fileData, ParameterType.RequestBody);
            IRestResponse resp3 = _client.Execute(uploadReq);

            if (resp3.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("file " + strFile + " uploaded");

                string responseString = resp3.Content;

                int len = responseString.Length;
                string id = "\"id\" : \"";
                int index = responseString.IndexOf(id) + id.Length;
                responseString = responseString.Substring(index, len - index - 1);
                int index2 = responseString.IndexOf("\"");
                string urn = responseString.Substring(0, index2);
                Console.WriteLine("file id :" + urn);
                byte[] bytes = Encoding.UTF8.GetBytes(urn);
                string urn64 = Convert.ToBase64String(bytes);
                RestRequest bubleReq = new RestRequest();
                bubleReq.Resource = "viewingservice/v1/register";
                bubleReq.Method = Method.POST;
                bubleReq.AddParameter("Authorization", "Bearer " + _token, ParameterType.HttpHeader);
                bubleReq.AddParameter("Content-Type", "application/json;charset=utf-8", ParameterType.HttpHeader);
                string body2 = "{\"urn\":\"" + urn64 + "\"}";
                bubleReq.AddParameter("application/json", body2, ParameterType.RequestBody);
                fileUrn = urn64;
                Console.WriteLine("urn:" + urn64);
                IRestResponse BubbleResp = _client.Execute(bubleReq);
                if (BubbleResp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("file " + strFile + " Translation started");
                }
                else if (BubbleResp.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    Console.WriteLine("file " + strFile + " Translation already present");
                }
            }
            else
            {
                Console.WriteLine("file " + strFile + " upload failed");
            }
            RestRequest thumnail = new RestRequest();
            thumnail.Resource = "/viewingservice/v1/" + fileUrn;
            thumnail.Method = Method.GET;
            thumnail.AddParameter("Authorization", "Bearer " + _token, ParameterType.HttpHeader);
            IRestResponse thumbResp = _client.Execute(thumnail);
            if (thumbResp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                dynamic json = SimpleJson.DeserializeObject(thumbResp.Content);
                System.Collections.Generic.Dictionary<string, object>.KeyCollection keys = json.Keys;
                System.Collections.Generic.Dictionary<string, object>.ValueCollection Values = json.Values;
                for (int i = 0; i < Values.Count; i++)
                {
                    var key = keys.ElementAt(i);
                    var item = Values.ElementAt(i);
                    if (key is string && item is string)
                    {
                        Console.WriteLine((string)key + "=" + (string)item);

                        if (String.Compare((string)key, "progress") == 0)
                        {
                             Console.WriteLine((string)item);

                        }
                    }
                }

            }
            else
            {
                Console.WriteLine(thumbResp.Content);
            }

            string url = string.Format("http://viewer.autodesk.io/node/view-helper?urn={0}&token={1}", fileUrn, _token);

            Console.WriteLine(url);
        }
    }
}
