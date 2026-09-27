codeunit 50100 [|MyCodeunit|] implements "Http Client Handler"
{
    var
        RestClient: Codeunit "Rest Client";

    procedure Send(CurrHttpClientInstance: HttpClient; HttpRequestMessage: HttpRequestMessage; var HttpResponseMessage: HttpResponseMessage): Boolean
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
