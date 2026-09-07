codeunit 50100 MyCodeunit
{
    procedure [|GetValue|](Done: Boolean) Result: Integer
    begin
        repeat
            if Done then begin
                break;
            end;
        until TryFetch(Result);
    end;

    local procedure TryFetch(var Value: Integer): Boolean
    begin
        Value := 42;
        exit(true);
    end;
}