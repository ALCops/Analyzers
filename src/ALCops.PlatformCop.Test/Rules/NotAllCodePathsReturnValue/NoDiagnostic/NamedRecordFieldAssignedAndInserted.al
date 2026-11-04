table 50100 MyLedgerEntry
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; "Posting Date"; Date) { }
    }

    keys
    {
        key(PK; "Entry No.") { Clustered = true; }
    }

    procedure GetLastEntryNo(): Integer
    begin
        exit(0);
    end;
}

codeunit 50101 MyCodeunit
{
    local procedure [|CreateLedgerEntry|]() LedgerEntry: Record MyLedgerEntry
    begin
        LedgerEntry."Entry No." := LedgerEntry.GetLastEntryNo() + 1;
        LedgerEntry."Posting Date" := WorkDate();
        LedgerEntry.Insert();
    end;
}
