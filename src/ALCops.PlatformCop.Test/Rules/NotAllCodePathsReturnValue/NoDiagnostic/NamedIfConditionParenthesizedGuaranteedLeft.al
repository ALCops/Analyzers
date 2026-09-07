codeunit 50100 MyCodeunit
{
    procedure [|GetValueWithParentheses|](Enabled: Boolean) Result: Integer
    begin
        if (TryFetch(Result) and Enabled) then begin
            Result := 1;
        end;
    end;

    procedure [|GetValueWithNotAndParentheses|](Enabled: Boolean) Result: Integer
    begin
        if not (TryFetch(Result) and Enabled) then begin
            Result := 1;
        end;
    end;

    local procedure TryFetch(var Value: Integer): Boolean
    begin
        Value := 42;
        exit(true);
    end;
}