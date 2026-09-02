codeunit 50100 ScalarClearBeforeRead
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        Clear(MyValue);
        Message('%1', MyValue);
    end;
}
