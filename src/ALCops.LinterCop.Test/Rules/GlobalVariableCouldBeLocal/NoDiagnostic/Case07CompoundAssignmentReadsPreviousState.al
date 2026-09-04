codeunit 50100 VariableScopeCase07
{
    var
        [|MyCounter|]: Integer;

    local procedure ShowNextNumber()
    begin
        MyCounter += 1;
        Message('%1', MyCounter);
    end;
}
