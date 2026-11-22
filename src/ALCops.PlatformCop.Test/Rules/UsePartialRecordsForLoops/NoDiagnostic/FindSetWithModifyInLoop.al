codeunit 50100 MyCodeunit
{
    procedure UpdateVendorLedgerEntries()
    var
        MyTable: Record MyTable;
    begin
        if [|MyTable.FindSet()|] then
            repeat
                MyTable.Modify(true);
            until MyTable.Next() = 0;
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    keys
    {
        key(PK; MyField) { }
    }
}
