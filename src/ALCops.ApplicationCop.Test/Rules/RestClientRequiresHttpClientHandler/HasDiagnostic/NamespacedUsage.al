namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50100 [|MyCodeunit|]
{
    var
        RestClient: Codeunit MyPublisher.MyExtension.MyAppDomain."Rest Client";
}

codeunit 2350 "Rest Client"
{
}

interface "Http Client Handler"
{
    procedure Send(CurrHttpClientInstance: HttpClient; HttpRequestMessage: HttpRequestMessage; var HttpResponseMessage: HttpResponseMessage): Boolean;
}
