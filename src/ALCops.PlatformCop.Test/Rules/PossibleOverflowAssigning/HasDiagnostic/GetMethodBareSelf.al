table 50100 MyTable
{
    fields
    {
        field(1; MyField; Code[20]) { }
    }

    procedure MyProcedure()
    begin
        Get([|'ABCDEFGHIJKLMNOPQRSTU'|]); // 21 characters, exceeds Code[20]
    end;
}
