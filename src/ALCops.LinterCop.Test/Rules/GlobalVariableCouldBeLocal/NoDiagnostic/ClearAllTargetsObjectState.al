codeunit 50100 ClearAllTargetsObjectState
{
    var
        [|MyValue|]: Integer;

    local procedure ResetValue()
    begin
        MyValue := 42;
        ClearAll();
        Message('%1', MyValue);
    end;
}
