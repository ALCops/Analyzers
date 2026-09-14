codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MySetup: Record MySetup;
        RecRef: RecordRef;
    begin
        RecRef.SetTable(MySetup);
        [|RecRef.FindFirst()|];
    end;
}

table 50100 MySetup
{
    fields
    {
        field(1; "Primary Key"; Code[10]) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
