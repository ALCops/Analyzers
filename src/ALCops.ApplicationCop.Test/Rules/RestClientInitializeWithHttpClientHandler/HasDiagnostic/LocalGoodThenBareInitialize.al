codeunit 50100 MyCodeunit
{
    procedure DoSomething()
    var
        RestClient: Codeunit "Rest Client";
        MyHandler: Codeunit MyHttpClientHandler;
    begin
        RestClient.Initialize(MyHandler);
        RestClient.Get('https://example.com');
        [|RestClient.Initialize()|];
        RestClient.Get('https://example.com');
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

codeunit 50101 MyHttpClientHandler implements "Http Client Handler"
{
    procedure Send(CurrHttpClientInstance: HttpClient; HttpRequestMessage: HttpRequestMessage; var HttpResponseMessage: HttpResponseMessage): Boolean
    begin
    end;
}
