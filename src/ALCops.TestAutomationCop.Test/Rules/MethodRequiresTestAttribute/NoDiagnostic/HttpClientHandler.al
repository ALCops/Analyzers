codeunit 50100 MyCodeunit
{
    Subtype = Test;

    [HttpClientHandler]
    procedure [|MyHttpClientHandler(request: TestHttpRequestMessage; var response: TestHttpResponseMessage): Boolean|]
    begin
    end;
}
