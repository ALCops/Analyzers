codeunit 50100 [|MyCodeunit|]
{
    var
        RestClient: Codeunit "Rest Client";
        OtherRestClient: Codeunit "Rest Client";

    procedure DoSomething()
    var
        LocalRestClient: Codeunit "Rest Client";
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
