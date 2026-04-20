table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

tableextension 50100 MyTableExt extends MyTable
{
    procedure MyProcedure(var MyTableParam: Record MyTable)
    begin
    end;

    procedure CallMyProcedure()
    begin
        MyProcedure([|Rec|]);
    end;
}
