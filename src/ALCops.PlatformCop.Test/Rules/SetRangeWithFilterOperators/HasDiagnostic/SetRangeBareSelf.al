table 50100 MyTable
{
    fields
    {
        field(1; MyField; Code[20]) { }
    }

    procedure MyProcedure()
    begin
        [|SetRange(MyField, '<>%1', 'test')|];
    end;
}
