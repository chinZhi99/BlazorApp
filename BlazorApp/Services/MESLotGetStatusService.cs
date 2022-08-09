using RestSharp;
using RestSharp.Serializers;
using System.Xml;

namespace BlazorApp.Services
{
    public interface IMESLotGetStatusService
    {
        string Send(string endpoint, string body);
    }

    public class MESLotGetStatusService: IMESLotGetStatusService
    {
        public string Send(string endpoint, string body) 
        {
            string _endpoint = endpoint;
            var options = new RestClientOptions(_endpoint)
            {
                ThrowOnAnyError = true,
                UseDefaultCredentials = true
                //MaxTimeout = 10000
            };
            var client = new RestClient(options);
            var request = new RestRequest();

            //convert input payload to xml doc
            XmlDocument xmlBody = new XmlDocument();
            xmlBody.LoadXml(body);
            Console.WriteLine(xmlBody);

            Console.WriteLine(body);
            //request.AddStringBody(body, ContentType.Xml);
            request.AddXmlBody(xmlBody);
            Console.WriteLine(request);

            var response = client.PostAsync(request).Result;
            Console.WriteLine(response.StatusCode.ToString() + " " + response.Content.ToString());
            Console.Read();

            return response.Content.ToString();
        }
    }
}
