codeunit 50100 TryFunctionNonRecordBuiltIns
{
    [TryFunction]
    local procedure TryWork()
    var
        Names: List of [Text];
        Builder: TextBuilder;
        Client: HttpClient;
        Response: HttpResponseMessage;
    begin
        [|Names.Insert(1, 'x')|];
        [|Builder.Insert(1, 'x')|];
        [|Client.Delete('https://example.com', Response)|];
    end;
}
