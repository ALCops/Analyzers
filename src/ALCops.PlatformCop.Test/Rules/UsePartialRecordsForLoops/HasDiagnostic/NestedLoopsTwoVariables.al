codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Decimal
    var
        MyTable: Record MyTable;
        MyLine: Record MyLine;
        Total: Decimal;
    begin
        if [|MyTable.FindSet()|] then
            repeat
                MyLine.SetRange("Document No.", MyTable."No.");
                if [|MyLine.FindSet()|] then
                    repeat
                        Total += MyLine.Amount;
                    until MyLine.Next() = 0;
            until MyTable.Next() = 0;
        exit(Total);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}

table 50101 MyLine
{
    fields
    {
        field(1; "Document No."; Code[20]) { }
        field(2; "Line No."; Integer) { }
        field(3; Amount; Decimal) { }
    }

    keys
    {
        key(PK; "Document No.", "Line No.") { }
    }
}
