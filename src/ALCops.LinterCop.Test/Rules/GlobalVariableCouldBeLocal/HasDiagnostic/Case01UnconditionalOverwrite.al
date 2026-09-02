codeunit 50100 VariableScopeCase01
{
    var
        [|MyGlobalVariable|]: Integer;

    local procedure MyDummyProcedure()
    begin
        MyGlobalVariable := 1;
        Message('%1', MyGlobalVariable);
    end;

    local procedure MyOtherDummyProcedure()
    begin
        Message('Hello World');
    end;
}
