codeunit 50100 MyCodeunit
{
    procedure [|GetValue|]() Result: Integer
    var
        Buffer: Integer;
    begin
        repeat
            Buffer := Buffer + 1;
        until TryFetch(Result);
    end;

    local procedure TryFetch(var Value: Integer): Boolean
    begin
        Value := 42;
        exit(true);
    end;
}
