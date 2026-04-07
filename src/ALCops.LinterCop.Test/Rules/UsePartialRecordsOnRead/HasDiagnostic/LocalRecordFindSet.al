codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Decimal
    var
        MyTable: Record MyTable;
        Total: Decimal;
    begin
        [|MyTable.FindSet()|];
        repeat
            Total += MyTable.Amount;
        until MyTable.Next() = 0;
        exit(Total);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; Amount; Decimal) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
