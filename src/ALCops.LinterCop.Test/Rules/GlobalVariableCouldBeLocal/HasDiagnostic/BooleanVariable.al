codeunit 50100 VariableScopeBoolean
{
    var
        [|MyTestVariable|]: Boolean;

    local procedure MyTestProcedure()
    begin
        MyTestVariable := true;
        Message('%1', MyTestVariable);
    end;
}
