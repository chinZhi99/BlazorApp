
using BlazorApp.Services;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var faimservice = new FaimService();
            var message = @"<Envelope>
  <Header>
    <OperationName>MES.LotGetStatus</OperationName>
    <UserContext>
      <UserID>username</UserID>
    </UserContext>
    <Properties>
      <Property Name=""BackendEnv"" Value=""""/>
      <Property Name=""Facility"" Value=""A04""/>
      <Property Name=""UserName"" Value=""""/>
      <Property Name=""Password"" Value=""""/>
    </Properties>
  </Header>
  <Body>
    <OperationInfo OperationName=""MES.LotGetStatus"">
      <LotContext>
        <LotID>TESTLOTA</LotID>
      </LotContext>
    </OperationInfo>
  </Body>
</Envelope> ";
            var response = faimservice.Send("net.tcp://atdvdwfaimwbn1.amr.corp.intel.com:9300/FAIMRouterService/Router.svc", message).Result;
        }

        [Fact]
        public void testMesLotGetStatusService()
        {
            var meslotgetstatusservice = new MESLotGetStatusService();
            string body =
@"<Envelope>
  <Header>
    <OperationName>MES.LotGetStatus</OperationName> 
    <UserContext>
      <UserID>username</UserID>
    </UserContext>
    <Properties>
      <Property Name=""BackendEnv"" Value="""" /> 
      <Property Name=""Facility"" Value=""A04"" />
	  <Property Name=""UserName"" Value="""" />
	  <Property Name=""Password"" Value="""" />
    </Properties> 
  </Header> 
  <Body>
    <OperationInfo OperationName=""MES.LotGetStatus"">
      <LotContext>
        <LotID>TESTLOTA</LotID> 
      </LotContext>
    </OperationInfo>
  </Body>
</Envelope>";
            var response = meslotgetstatusservice.Send("http://143.182.152.194/FAIMRestRouterService/FAIMRestRouterService.svc/ProcessRestXML", body);
        }
    }
}