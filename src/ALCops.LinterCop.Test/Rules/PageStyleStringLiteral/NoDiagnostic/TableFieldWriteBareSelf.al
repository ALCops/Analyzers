table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[50]) { }
    }

    procedure MyProcedure()
    begin
        MyField := [|'Standard'|];
    end;
}
