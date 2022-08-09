using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Linq;

namespace BlazorApp.Services
{
    public interface IFaimService
    {
        Task<string> Send(string endpoint, string message);
    }
    public class FaimService : IFaimService
    {
        public async Task<string> Send(string endpoint, string message)
        {
            var faimInput = new FaimMessageModel(message);

            using var client = new FaimServiceClient(new EndpointAddress(endpoint));
            using var rm = await client.ProcessMessageAsync(AsWcfMessage(message, faimInput.Operation));
            if (rm.IsFault)
            {
                throw new Exception("An error occurred.");
            }

            return new FaimResponseMessageModel(rm).AsString();
        }

        public Message AsWcfMessage(string message, string operation)
        {
            var wcfMessage = Message.CreateMessage(MessageVersion.Soap12WSAddressing10, "ProcessMessage", message);

            wcfMessage.Headers.Add(MessageHeader.CreateHeader("FAIMOperation", string.Empty, operation));
            wcfMessage.Headers.Add(MessageHeader.CreateHeader("OperationName", string.Empty, operation));
            wcfMessage.Headers.Add(new TIFWMessageHeader());

            return wcfMessage;
        }
    }

    public class TIFWMessageHeader : MessageHeader
    {
        public string TransactionId { get; set; }

        public TIFWMessageHeader()
        {
            TransactionId = $"{Environment.MachineName}.{Guid.NewGuid()}";
        }

        public override string Name => nameof(TIFWMessageHeader);
        public override string Namespace => string.Empty;

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            writer.WriteAttributeString("TransactionSuccess", true.ToString());
            writer.WriteAttributeString("TimeStamp", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("TransactionId", TransactionId);
            writer.WriteAttributeString("TransactionHop", "1");
            writer.WriteAttributeString("HopOrder", 1.ToString());
            writer.WriteAttributeString("PayloadVersion", string.Empty);
            writer.WriteAttributeString("Environment", string.Empty);
        }
    }

    public class FaimResponseMessageModel
    {
        private readonly Message _message;

        public FaimResponseMessageModel(Message message)
        {
            _message = message;
        }

        public string AsString()
        {
            if (IsBinaryResponse() == false)
                return _message.GetBody<string>();

            var response = new FaimMessageModel(TemplateForBinaryResponses());
            response.AddBinaryResponse(_message.GetBody<byte[]>());
            return response.AsString();
        }

        private string TemplateForBinaryResponses()
        {
            var foundResponseTemplate = GetHeader<string>("FAIMHeader", out var responseTemplate);

            if (foundResponseTemplate == false)
                throw new Exception("Response from service is missing response template.");

            return responseTemplate!;
        }

        private bool IsBinaryResponse()
        {
            return _message.Headers.FindHeader("HasBinaryData", "") > -1;
        }

        private bool GetHeader<T>(string name, out T? content)
        {
            content = default;
            var index = _message.Headers.FindHeader(name, "");

            if (index < 0) return false;

            content = _message.Headers.GetHeader<T>(index);
            return true;
        }
    }

    public class FaimMessageModel
    {
        private readonly XElement _doc;
        private readonly XElement? _header;
        private readonly string _message;

        public string Operation => _header.ElementValueByName("OperationName");

        public FaimMessageModel(string message, bool removeNamespaces = true)
        {
            _message = message;

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentOutOfRangeException(nameof(message), "Message is invalid.");

            _doc = removeNamespaces
                ? XElement.Parse(message).RemoveNamespaces(e => IsFaDotNetMessage(e) == false)
                : XElement.Parse(message);

            if (_doc.ElementExistsByName("Header") == false)
                throw new Exception("Input data has format issue, header cannot be found");

            _header = _doc.Elements().SingleOrDefault(e => e.Name == "Header");
        }

        public void AddBinaryResponse(byte[] data)
        {
            var body = _doc.Elements().Single(e => e.Name == "Body");

            var operationInfo = body.Elements().SingleOrDefault(e => e.Name == "OperationInfo");
            if (operationInfo is null)
            {
                operationInfo = XElement.Parse($"<OperationInfo OperationName=\"{Operation}\" />");
                body.Add(operationInfo);
            }

            var binaryData = new XElement("BinaryData")
            {
                Value = Convert.ToBase64String(data)
            };

            operationInfo.Add(binaryData);
        }

        private bool IsFaDotNetMessage(XElement element)
        {
            return element.ToString().ToUpper().Contains("URN:INTEL-FABAUTO-FA300");
        }

        public Message AsWcfMessage()
        {
            var wcfMessage = Message.CreateMessage(MessageVersion.Soap12WSAddressing10, "ProcessMessage", _message);

            wcfMessage.Headers.Add(MessageHeader.CreateHeader("FAIMOperation", string.Empty, Operation));
            wcfMessage.Headers.Add(MessageHeader.CreateHeader("OperationName", string.Empty, Operation));
            wcfMessage.Headers.Add(new TIFWMessageHeader());

            return wcfMessage;
        }

        public string AsString()
        {
            return _doc.ToString();
        }

        public class TIFWMessageHeader : MessageHeader
        {
            public string TransactionId { get; set; }

            public TIFWMessageHeader()
            {
                TransactionId = $"{Environment.MachineName}.{Guid.NewGuid()}";
            }

            public override string Name => nameof(TIFWMessageHeader);
            public override string Namespace => string.Empty;

            protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
            {
                writer.WriteAttributeString("TransactionSuccess", true.ToString());
                writer.WriteAttributeString("TimeStamp", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("TransactionId", TransactionId);
                writer.WriteAttributeString("TransactionHop", "1");
                writer.WriteAttributeString("HopOrder", 1.ToString());
                writer.WriteAttributeString("PayloadVersion", string.Empty);
                writer.WriteAttributeString("Environment", string.Empty);
            }
        }
    }

    public static class FaimXElementExtensions
    {
        public static List<XElement> ExtractList(this XElement parent, string collectionElementName, string elementName)
        {
            return parent
                       ?.Elements()
                       .SingleOrDefault(e => e.Name == collectionElementName)
                       ?.Elements()
                       .Where(e => e.Name == elementName)
                       .ToList()
                   ?? new List<XElement>();
        }

        public static void AddListItem(this XElement parent, string collection, XElement item)
        {
            if (parent.Element(collection) == null)
            {
                parent.Add(new XElement(collection));
            }
            parent.Element(collection)?.Add(item);
        }

        public static string ElementValueByName(this XElement parent, string elementName)
        {
            return parent.Elements().SingleOrDefault(e => e.Name == elementName)?.Value;
        }

        public static T ElementValueByName<T>(this XElement parent, string elementName, T defaultValue = default)
        {
            var value = parent.Elements().SingleOrDefault(e => e.Name == elementName)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static bool ElementExistsByName(this XElement parent, string elementName)
        {
            return parent.Elements().Any(e => e.Name == elementName);
        }

        public static T ValueFromNameValueCollection<T>(this List<XElement> elements, string name)
        {
            var value = elements.SingleOrDefault(p => p.Attribute("Name")?.Value.ToUpper() == name.ToUpper())
                ?.Attribute("Value")?.Value;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static List<KeyValuePair<string, string>> AsNameValueList(this List<XElement> elements, string keyName, string valueName)
        {
            return elements.Select(e => new KeyValuePair<string, string>(e.Attribute(keyName)?.Value, e.Attribute(valueName)?.Value)).ToList();
        }

        public static bool ExistsInNameValueCollection(this List<XElement> elements, string name)
        {
            return elements.Count(p => p.Attribute("Name")?.Value.ToUpper() == name.ToUpper()) == 1;
        }

        public static bool ExistsInNameValueCollection(this List<XElement> elements, Func<string, bool> matchName)
        {
            return elements.Any(p => matchName(p.Attribute("Name")?.Value.ToUpper()));
        }

        public static XElement RemoveNamespaces(this XElement doc, Func<XElement, bool> removeNamespaces)
        {
            foreach (var e in doc.DescendantsAndSelf())
            {
                if (removeNamespaces(e) == false) continue;

                if (e.Name.Namespace != XNamespace.None)
                {
                    e.Name = XNamespace.None.GetName(e.Name.LocalName);
                }
                if (e.Attributes().Any(a => a.IsNamespaceDeclaration || a.Name.Namespace != XNamespace.None))
                {
                    e.ReplaceAttributes(e.Attributes().Select(a => a.IsNamespaceDeclaration ? null : a.Name.Namespace != XNamespace.None ? new XAttribute(XNamespace.None.GetName(a.Name.LocalName), a.Value) : a));
                }
            }
            return doc;
        }
    }

    [ServiceContract(ConfigurationName = "IProcessMsg")]
    public interface IFaimMessageProcessor
    {
        [OperationContract(Action = "ProcessMessage", ReplyAction = "ProcessMessageResponse")]
        Task<Message> ProcessMessageAsync(Message request);
    }

    public class FaimServiceClient : ClientBase<IFaimMessageProcessor>, IFaimMessageProcessor
    {
        public FaimServiceClient(EndpointAddress remoteAddress) : base(Binding(), remoteAddress)
        {
        }
        public Task<Message> ProcessMessageAsync(Message request)
        {
            return Channel.ProcessMessageAsync(request);
        }

        private static Binding Binding()
        {
            return new NetTcpBinding
            {
                Name = "TcpBindingConfig",
                CloseTimeout = TimeSpan.FromMinutes(3),
                OpenTimeout = TimeSpan.FromMinutes(3),
                ReceiveTimeout = TimeSpan.FromMinutes(3),
                SendTimeout = TimeSpan.FromMinutes(3),
                MaxBufferSize = 2147483647,
                MaxReceivedMessageSize = 2147483647,
                TransferMode = TransferMode.Buffered,
                ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxDepth = 2147483647,
                    MaxStringContentLength = 2147483647,
                    MaxArrayLength = 2147483647,
                    MaxBytesPerRead = 2147483647,
                    MaxNameTableCharCount = 2147483647
                }
            };
        }

    }
}
