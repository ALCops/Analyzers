codeunit 50100 MyCodeunit
{
    var
        APIAuthenticationToken: SecretText;

    internal procedure [|GetAPIAuthenticationToken|]() Value: SecretText
    begin
        if not this.TryGetIsolatedStorage(Value, APIAuthenticationToken) then
            Clear(APIAuthenticationToken);
    end;

    local procedure TryGetIsolatedStorage(var Value: SecretText; StorageKey: SecretText): Boolean
    begin
        Value := StorageKey;
        exit(true);
    end;
}