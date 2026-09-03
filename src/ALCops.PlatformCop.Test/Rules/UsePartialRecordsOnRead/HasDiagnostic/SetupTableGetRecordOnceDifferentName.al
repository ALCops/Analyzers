codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MySetup: Record MySetup;
    begin
        [|MySetup.Get()|];
        exit(MySetup.MyField);
    end;
}

table 50100 MySetup
{
    fields
    {
        field(1; "Setup Code"; Code[20]) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Setup Code") { }
    }

    procedure GetRecordsOnce()
    begin
    end;
}
