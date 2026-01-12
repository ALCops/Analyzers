codeunit 50100 MyCodeunit
{
    var
        FromRec: Record MyTableA;

    procedure MyProcedure()
    var
        ToRec: Record MyTableB;
    begin
        [|ToRec.TransferFields(FromRec)|];
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