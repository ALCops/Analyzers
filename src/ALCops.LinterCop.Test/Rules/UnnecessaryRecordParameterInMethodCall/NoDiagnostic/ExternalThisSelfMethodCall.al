table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }

    procedure MyProcedure(MyTableParam: Record MyTable)
    begin
    end;

    procedure CallMyProcedure()
    begin
        this.MyProcedure([|this|]);
    end;
}
