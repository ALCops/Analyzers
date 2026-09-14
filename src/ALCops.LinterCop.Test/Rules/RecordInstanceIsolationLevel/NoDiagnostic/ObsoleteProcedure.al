table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    [Obsolete('Replaced by ReadIsolation.', '25.0')]
    procedure MyProcedure()
    begin
        [|Rec.LockTable();|]
    end;
}
