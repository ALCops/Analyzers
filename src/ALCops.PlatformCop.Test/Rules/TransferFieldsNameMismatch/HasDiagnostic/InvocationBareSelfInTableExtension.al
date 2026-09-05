table 50100 MyTableA
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyFieldA; Integer) { }
    }
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyFieldB; Integer) { }
    }
}

tableextension 50100 MyExtension extends MyTableA
{
    procedure MyProcedure()
    var
        FromRec: Record MyTableB;
    begin
        [|TransferFields(FromRec)|];
    end;
}
