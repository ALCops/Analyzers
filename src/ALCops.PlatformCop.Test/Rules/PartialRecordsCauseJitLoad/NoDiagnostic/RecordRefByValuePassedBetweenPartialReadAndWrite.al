codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        RecRef: RecordRef;
    begin
        RecRef.Open(Database::MyTable);
        [|RecRef.SetLoadFields(1)|];
        RecRef.FindFirst();
        Reload(RecRef);
        RecRef.Delete();
    end;

    local procedure Reload(RecRef: RecordRef)
    begin
        RecRef.SetLoadFields();
        RecRef.FindFirst();
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Description; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}
