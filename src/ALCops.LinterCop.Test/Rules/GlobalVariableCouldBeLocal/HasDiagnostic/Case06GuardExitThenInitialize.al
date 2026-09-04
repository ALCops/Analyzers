codeunit 50100 VariableScopeCase06
{
    var
        [|MyGuardedValue|]: Integer;

    local procedure ShowGuardedValue(ShouldContinue: Boolean)
    begin
        if not ShouldContinue then
            exit;

        MyGuardedValue := 42;
        Message('%1', MyGuardedValue);
    end;
}
