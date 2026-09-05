table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    procedure MyProcedure()
    begin
        [|Rec.Validate(MyField, 1)|];
    end;
}
