codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.Get('BUS', 'PROD')|];
        exit(MyTable.MyField);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Bus. Posting Group"; Code[20]) { }
        field(2; "Prod. Posting Group"; Code[20]) { }
        field(3; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Bus. Posting Group", "Prod. Posting Group") { }
    }
}
