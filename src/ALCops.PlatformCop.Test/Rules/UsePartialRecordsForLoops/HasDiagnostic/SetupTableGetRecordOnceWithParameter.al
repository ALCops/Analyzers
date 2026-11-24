codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MySetup: Record MySetup;
        Result: Text;
    begin
        [|MySetup.FindSet()|];
        repeat
            Result += MySetup.MyField;
        until MySetup.Next() = 0;
        exit(Result);
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

    procedure GetRecordOnce(ForceReload: Boolean)
    begin
    end;
}
