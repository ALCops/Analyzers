table 50100 MyTable
{
    fields
    {
        field(1; MyField; Code[20]) { }
    }

    procedure MyProcedure()
    begin
        Rec.[|Get|]();
    end;
}
