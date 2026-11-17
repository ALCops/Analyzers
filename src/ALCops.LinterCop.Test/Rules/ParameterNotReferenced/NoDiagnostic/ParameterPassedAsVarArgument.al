codeunit 50100 ParameterPassedAsVarArgument
{
    procedure MyProcedure([|Name: Text|])
    begin
        Update(Name);
    end;

    local procedure Update(var Value: Text)
    begin
        Value := 'x';
    end;
}
