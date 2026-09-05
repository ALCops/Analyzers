table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
        field(2; MyField2; Code[20]) { }
    }

    keys
    {
        key(PK; MyField, MyField2) { }
    }

    procedure MyProcedure()
    begin
        [|Get(1)|];
    end;
}
