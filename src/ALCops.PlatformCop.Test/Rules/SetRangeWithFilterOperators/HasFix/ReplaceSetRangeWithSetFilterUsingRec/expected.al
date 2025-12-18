table 50100 MyTable
{
    fields
    {
        field(1; MyField; Code[20]) { }
    }

    procedure MyProcedure()
    var
        MyCode: Code[20];
    begin
        Rec.SetFilter(MyField, '<>%1', MyCode);
    end;
}