codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        FromRec: Record MyTableA;
        ToRec: Record MyTableB;
    begin
        [|ToRec.TransferFields(FromRec, true)|];
    end;
}

table 50100 MyTableA
{
    fields
    {
        [|field(1; "Primary Key"; Code[20]) { }|]
        field(2; MyField; Integer) { }
    }
}

table 50101 MyTableB
{
    fields
    {
        [|field(1; "Other Primary Key"; Code[20]) { }|] // Same ID (1) as in MyTableA, different name
        field(2; MyField; Integer) { }
    }
}