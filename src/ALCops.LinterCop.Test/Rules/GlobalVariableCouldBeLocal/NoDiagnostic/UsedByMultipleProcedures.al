codeunit 50100 UsedByMultipleProcedures
{
    var
        [|MyValue|]: Integer;

    local procedure SetValue()
    begin
        MyValue := 10;
    end;

    local procedure ShowValue()
    begin
        Message('%1', MyValue);
    end;
}
