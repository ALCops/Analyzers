table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    trigger OnInsert()
    begin
        [|LockTable();|]
    end;
}
