codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        FromRec: Record MyTableA;
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
        [|field(2; MyField; Code[20]) { }|]
    }
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(2; MyField; Text[20]) { }|] // Same ID (2) as in MyTableA, where a Code can safely convert into a Text
    }
}