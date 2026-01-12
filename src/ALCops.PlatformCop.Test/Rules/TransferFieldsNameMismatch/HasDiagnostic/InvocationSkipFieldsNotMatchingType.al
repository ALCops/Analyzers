codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        FromRec: Record MyTableA;
        ToRec: Record MyTableB;
    begin
        [|ToRec.TransferFields(FromRec, false, false)|]; // SkipFieldsNotMatchingType (third boolean) is set to false
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