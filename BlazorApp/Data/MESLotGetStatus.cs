using RestSharp;

namespace BlazorApp.Data
{
    public class MESLotGetStatus
    {
        public void getSomething()
        {
            var options = new RestClientOptions("http://143.182.152.194/FAIMRestRouterService/FAIMRestRouterService.svc/ProcessRestXML")
            {
                ThrowOnAnyError = true,
                MaxTimeout = 5000
            };
            var client = new RestClient(options);
    
            var request = new RestRequest();
            request.AddHeader("Content-Type", "application/xml");
            request.AddHeader("Cookie", "ASP.NET_SessionId=mqktqeivrkhbai2yrjfmjq5r");
            var body = @"<Envelope>
" + "\n" +
            @"  <Header>
" + "\n" +
            @"    <OperationName>MES.LotGetStatus</OperationName>
" + "\n" +
            @"    <UserContext>
" + "\n" +
            @"      <UserID>username</UserID>
" + "\n" +
            @"    </UserContext>
" + "\n" +
            @"    <Properties>
" + "\n" +
            @"      <Property Name=""BackendEnv"" Value="""" />
" + "\n" +
            @"      <Property Name=""Facility"" Value=""A04"" />
" + "\n" +
            @"	  <Property Name=""UserName"" Value="""" />
" + "\n" +
            @"	  <Property Name=""Password"" Value="""" />
" + "\n" +
            @"    </Properties>
" + "\n" +
            @"  </Header>
" + "\n" +
            @"  <Body>
" + "\n" +
            @"    <OperationInfo OperationName=""MES.LotGetStatus"">
" + "\n" +
            @"      <LotContext>
" + "\n" +
            @"        <LotID>TESTLOTA</LotID>
" + "\n" +
            @"      </LotContext>
" + "\n" +
            @"    </OperationInfo>
" + "\n" +
            @"  </Body>
" + "\n" +
            @"</Envelope>
" + "\n" +
            @"";

            request.AddParameter("application/xml", body, ParameterType.RequestBody);

            var response = client.Post(request);

            Console.WriteLine(response.Content);

            Console.Read();
        }
    }
}
