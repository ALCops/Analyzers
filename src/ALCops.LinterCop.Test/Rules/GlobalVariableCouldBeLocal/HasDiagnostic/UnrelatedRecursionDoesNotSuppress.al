codeunit 50100 UnrelatedRecursion
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        MyValue := 10;
        Message('%1', MyValue);
    end;

    local procedure UnrelatedRecursiveProcedure(Number: Integer)
    begin
        if Number > 0 then
            UnrelatedRecursiveProcedure(Number - 1);
    end;
}
