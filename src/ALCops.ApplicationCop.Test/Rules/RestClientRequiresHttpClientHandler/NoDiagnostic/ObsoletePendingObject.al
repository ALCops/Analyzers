codeunit 50100 [|MyCodeunit|]
{
    ObsoleteState = Pending;
    ObsoleteReason = 'Replaced by a newer implementation.';

    var
        RestClient: Codeunit "Rest Client";
}

codeunit 2350 "Rest Client"
{
}

interface "Http Client Handler"
{
    procedure Send(CurrHttpClientInstance: HttpClient; HttpRequestMessage: HttpRequestMessage; var HttpResponseMessage: HttpResponseMessage): Boolean;
}
