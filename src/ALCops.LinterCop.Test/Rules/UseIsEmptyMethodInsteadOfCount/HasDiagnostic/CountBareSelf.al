table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
    }

    procedure MyProcedure()
    begin
        if [|Count() = 0|] then;
    end;
}
