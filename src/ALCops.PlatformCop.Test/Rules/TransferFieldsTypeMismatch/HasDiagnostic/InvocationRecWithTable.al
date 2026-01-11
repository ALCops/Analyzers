table 50100 MyTableA
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(2; MyField; Integer) { }|]
    }

    procedure MyProcedure()
    var
        FromRec: Record MyTableB;
    begin
        [|Rec.TransferFields(FromRec)|];
    end;
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(2; MyField; Boolean) { }|] // Same ID (2) as in MyTableA, different type
    }
}