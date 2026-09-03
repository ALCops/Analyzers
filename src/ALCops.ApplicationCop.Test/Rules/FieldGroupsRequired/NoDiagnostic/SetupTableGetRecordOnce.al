table 50100 [|MySetup|]
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

    local procedure GetRecordOnce()
    begin
    end;
}