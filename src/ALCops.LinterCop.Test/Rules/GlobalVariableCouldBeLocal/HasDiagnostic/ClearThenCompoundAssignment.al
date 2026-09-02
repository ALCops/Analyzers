codeunit 50100 ClearThenCompoundAssignment
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        Clear(MyValue);
        MyValue += 1;
        Message('%1', MyValue);
    end;
}
