codeunit 50100 MyCodeunit
{
    procedure [|GetValueWithAnd|](Enabled: Boolean; OtherEnabled: Boolean) Result: Integer
    begin
        if TryFetch(Result) and Enabled and OtherEnabled then
            Result := 1;
    end;

    procedure [|GetValueWithOr|](Enabled: Boolean; OtherEnabled: Boolean) Result: Integer
    begin
        if TryFetch(Result) or Enabled or OtherEnabled then
            Result := 1;
    end;

    local procedure TryFetch(var Value: Integer): Boolean
    begin
        Value := 42;
        exit(true);
    end;
}