codeunit 50100 MyCodeunit
{
    var
        [|MyVariable|]: Integer;
        [|MyRecord|]: Record MyTable;

    procedure MyProcedure()
    var
        [|LocalVar|]: Text;
    begin
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
    }
}
