codeunit 50100 SingleInstanceCodeunit
{
    SingleInstance = true;

    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        MyValue := 10;
        Message('%1', MyValue);
    end;
}
