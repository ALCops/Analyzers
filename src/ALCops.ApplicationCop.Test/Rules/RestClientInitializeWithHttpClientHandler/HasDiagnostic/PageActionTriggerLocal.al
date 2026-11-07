page 50100 MyPage
{
    SourceTable = MyTable;

    actions
    {
        area(Processing)
        {
            action(MyAction)
            {
                trigger OnAction()
                var
                    [|RestClient|]: Codeunit "Rest Client";
                begin
                    RestClient.Get('https://example.com');
                end;
            }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer)
        {
        }
    }
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
