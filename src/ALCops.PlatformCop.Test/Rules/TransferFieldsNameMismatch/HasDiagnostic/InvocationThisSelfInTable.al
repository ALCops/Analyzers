table 50100 MyTableA
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyFieldA; Integer) { }
    }

    procedure MyProcedure()
    var
        FromRec: Record MyTableB;
    begin
        [|this.TransferFields(FromRec)|];
    end;
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyFieldB; Integer) { }
    }
}
