codeunit 50100 MyCodeunit
{
    procedure DoSomething()
    var
        [|RestClient|]: Codeunit "Rest Client";
        IClient: Interface "My Client";
    begin
        IClient.Send(RestClient);
        RestClient.Get('https://example.com');
    end;
}

interface "My Client"
{
    procedure Send(var RC: Codeunit "Rest Client");
}

codeunit 50102 MyClient implements "My Client"
{
    procedure Send(var RC: Codeunit "Rest Client")
    begin
    end;
}

codeunit 2350 "Rest Client"
{
    procedure Initialize()
    begin
    end;

    procedure Initialize(HttpClientHandler: Interface "Http Client Handler")
    begin
    end;

    procedure Initialize(HttpAuthentication: Interface "Http Authentication")
    begin
    end;

    procedure Initialize(HttpClientHandler: Interface "Http Client Handler"; HttpAuthentication: Interface "Http Authentication")
    begin
    end;

    procedure Create(): Codeunit "Rest Client"
    begin
    end;

    procedure Create(HttpClientHandler: Interface "Http Client Handler"): Codeunit "Rest Client"
    begin
    end;

    procedure Create(HttpAuthentication: Interface "Http Authentication"): Codeunit "Rest Client"
    begin
    end;

    procedure Create(HttpClientHandler: Interface "Http Client Handler"; HttpAuthentication: Interface "Http Authentication"): Codeunit "Rest Client"
    begin
    end;

    procedure Get(RequestUri: Text)
    begin
    end;

    procedure SetBaseAddress(Url: Text)
    begin
    end;
}

interface "Http Client Handler"
{
    procedure Send(CurrHttpClientInstance: HttpClient; HttpRequestMessage: HttpRequestMessage; var HttpResponseMessage: HttpResponseMessage): Boolean;
}

interface "Http Authentication"
{
    procedure IsAuthenticationRequired(): Boolean;
}
