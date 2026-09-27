codeunit 50100 [|MyCodeunit|]
{
    procedure DoSomething()
    begin
        Codeunit.Run(Codeunit::"Rest Client");
    end;
}

codeunit 2350 "Rest Client"
{
}

interface "Http Client Handler"
{
    procedure Send(CurrHttpClientInstance: HttpClient; HttpRequestMessage: HttpRequestMessage; var HttpResponseMessage: HttpResponseMessage): Boolean;
}
