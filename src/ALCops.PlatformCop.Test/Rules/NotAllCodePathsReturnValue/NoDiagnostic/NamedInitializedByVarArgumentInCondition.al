codeunit 50100 MyCodeunit
{
    procedure [|GetValue|]() Result: Integer
    begin
        if not TryGetValue(Result) then
            Error('Value not found.');
    end;

    local procedure TryGetValue(var Value: Integer): Boolean
    begin
        Value := 42;
        exit(true);
    end;
}