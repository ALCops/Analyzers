codeunit 50100 MyUpgradeCodeunit
{
    Subtype = Upgrade;

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
        [|field(2; MyField; Integer) { }|]
    }
}

table 50101 MyTableB
{
    ObsoleteState = Removed;

    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(2; MyField; Boolean) { }|]
    }
}
