codeunit 50100 MyCodeunit
{
    procedure [|GetValue|](Flag: Boolean) Result: Integer
    begin
        if Flag ? TryFetch(Result) : TryFetch(Result) then begin
            exit;
        end;
    end;

    local procedure TryFetch(var Value: Integer): Boolean
    begin
        Value := 42;
        exit(true);
    end;
}