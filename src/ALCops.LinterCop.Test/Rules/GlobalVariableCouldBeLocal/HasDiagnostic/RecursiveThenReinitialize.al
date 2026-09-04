codeunit 50100 RecursiveThenReinitialize
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue(Number: Integer)
    begin
        if Number > 0 then
            ShowValue(Number - 1);

        MyValue := Number;
        Message('%1', MyValue);
    end;
}
