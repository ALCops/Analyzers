codeunit 50100 VariableScopeCase10
{
    var
        [|MyRecursiveValue|]: Integer;

    local procedure RecursiveProcedure(Number: Integer): Integer
    begin
        MyRecursiveValue := Number;

        if Number > 1 then
            RecursiveProcedure(Number - 1);

        exit(MyRecursiveValue);
    end;
}
