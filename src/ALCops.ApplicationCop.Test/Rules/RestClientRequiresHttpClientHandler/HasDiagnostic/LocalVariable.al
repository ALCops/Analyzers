codeunit 50100 [|MyCodeunit|]
{
    procedure DoSomething()
    var
        RestClient: Codeunit "Rest Client";
    begin
    end;
}

codeunit 2350 "Rest Client"
{
}

interface "Http Client Handler"
{
    procedure Send(CurrHttpClientInstance: HttpClient; HttpRequestMessage: HttpRequestMessage; var HttpResponseMessage: HttpResponseMessage): Boolean;
}
