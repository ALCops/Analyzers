table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    trigger OnInsert()
    begin
        // Serialize inserts on this table
        [|LockTable()|];
    end;
}
