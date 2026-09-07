codeunit 50100 MyCodeunit
{
    procedure [|GetValue|]() Result: Integer
    var
        Buffer: Integer;
    begin
        while TryFetch(Result) do
            Buffer := Buffer + 1;
    end;

    local procedure TryFetch(var Value: Integer): Boolean
    begin
        Value := 42;
        exit(false);
    end;
}
