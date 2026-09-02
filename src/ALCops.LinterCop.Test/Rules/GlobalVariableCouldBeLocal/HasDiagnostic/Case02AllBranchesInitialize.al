codeunit 50100 VariableScopeCase02
{
    var
        [|MyBranchValue|]: Integer;

    local procedure ShowBranchValue(UseFirstValue: Boolean)
    begin
        if UseFirstValue then
            MyBranchValue := 1
        else
            MyBranchValue := 2;

        Message('%1', MyBranchValue);
    end;
}
