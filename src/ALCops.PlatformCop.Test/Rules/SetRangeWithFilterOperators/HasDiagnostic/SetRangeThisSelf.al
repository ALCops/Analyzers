table 50100 MyTable
{
    fields
    {
        field(1; MyField; Code[20]) { }
    }

    procedure MyProcedure()
    begin
        [|this.SetRange(MyField, '<>%1', 'test')|];
    end;
}
