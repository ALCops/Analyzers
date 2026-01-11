codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        FromRec: Record MyTableA;
    begin
        [|GetToRec().TransferFields(FromRec)|];
    end;

    local procedure GetFromRec() FromRec: Record MyTableA
    begin
    end;

    local procedure GetToRec() ToRec: Record MyTableB
    begin
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
        [|field(2; MyField; Boolean) { }|] // Same ID (2) as in MyTableA, different type
    }
}