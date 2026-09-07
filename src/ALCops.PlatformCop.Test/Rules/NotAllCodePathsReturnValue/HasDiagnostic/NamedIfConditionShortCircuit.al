codeunit 50100 MyCodeunit
{
    procedure [|GetValueWithAnd|](Enabled: Boolean; OtherEnabled: Boolean) Result: Integer
    begin
        if Enabled and OtherEnabled and TryFetch(Result) then
            Result := 1;
    end;

    procedure [|GetValueWithOr|](Enabled: Boolean; OtherEnabled: Boolean) Result: Integer
    begin
        if Enabled or OtherEnabled or TryFetch(Result) then
            Result := 1;
    end;

    local procedure TryFetch(var Value: Integer): Boolean
    begin
        Value := 42;
        exit(true);
    end;
}
