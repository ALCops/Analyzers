codeunit 50100 ReadBeforeAssignment
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        Message('%1', MyValue);
        MyValue := 10;
    end;
}
