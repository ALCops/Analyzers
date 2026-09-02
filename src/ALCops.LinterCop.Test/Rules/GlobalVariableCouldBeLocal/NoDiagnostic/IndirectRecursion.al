codeunit 50100 IndirectRecursion
{
    var
        [|MyValue|]: Integer;

    local procedure RecursiveProcedure(Number: Integer): Integer
    begin
        MyValue := Number;

        if Number > 1 then
            Reenter(Number - 1);

        exit(MyValue);
    end;

    local procedure Reenter(Number: Integer)
    begin
        RecursiveProcedure(Number);
    end;
}
