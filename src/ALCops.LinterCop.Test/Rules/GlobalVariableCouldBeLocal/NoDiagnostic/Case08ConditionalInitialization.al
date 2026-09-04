codeunit 50100 VariableScopeCase08
{
    var
        [|MyConditionalValue|]: Integer;

    local procedure ShowConditionalValue(InitializeValue: Boolean)
    begin
        if InitializeValue then
            MyConditionalValue := 42;

        Message('%1', MyConditionalValue);
    end;
}
