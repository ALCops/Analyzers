codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        FromRec: Record MyTableA;
        ToRec: Record MyTableB;
        Helper: Codeunit MyHelper;
    begin
        [|Helper.TransferFields(FromRec, ToRec)|];
    end;
}

codeunit 50101 MyHelper
{
    procedure TransferFields(FromRec: Record MyTableA; ToRec: Record MyTableB)
    begin
        // User-defined procedure intentionally named TransferFields.
        // The analyzer must ignore this because it only analyzes built-in TransferFields on records.
    end;
}

table 50100 MyTableA
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(2; MyField; Integer) { }|]
    }
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(2; MyOtherField; Integer) { }|] // Same ID (2) as in MyTableA, different name
    }
}