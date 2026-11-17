codeunit 50100 ParameterAssignedOnly
{
    procedure MyProcedure([|Name: Text|])
    begin
        Name := 'x';
    end;
}
