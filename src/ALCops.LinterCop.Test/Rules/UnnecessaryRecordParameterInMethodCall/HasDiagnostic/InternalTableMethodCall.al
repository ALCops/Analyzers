table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }

    procedure MyProcedure(MyTable2: Record MyTable)
    begin
        MyProcedure([|Rec|]);
    end;
}
