codeunit 50100 MyCodeunit
{
    procedure [|GetValue|](Enabled: Boolean) Result: Integer
    var
        Buffer: Integer;
    begin
        while TryFetch(Result) and Enabled do
            Buffer := Buffer + 1;
    end;

    local procedure TryFetch(var Value: Integer): Boolean
    begin
        Value := 42;
        exit(true);
    end;
}