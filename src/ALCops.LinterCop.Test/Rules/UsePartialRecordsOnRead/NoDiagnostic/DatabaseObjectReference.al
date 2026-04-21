codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        ReservEntry: Record ReservEntry;
    begin
        ReservEntry.SetSourceFilter(DATABASE::MyTable, 0, 'TEST', 0, false);
        [|ReservEntry.FindFirst()|];
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}

table 50101 ReservEntry
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; "Source Type"; Integer) { }
        field(3; "Source Subtype"; Integer) { }
        field(4; "Source ID"; Code[20]) { }
        field(5; "Source Batch Name"; Code[10]) { }
        field(6; "Source Ref. No."; Integer) { }
    }

    keys
    {
        key(PK; "Entry No.") { }
    }

    procedure SetSourceFilter(SourceType: Integer; SourceSubtype: Integer; SourceID: Code[20]; SourceRefNo: Integer; SourceBatchName: Boolean)
    begin
        // stub
    end;
}
