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

    fieldgroups
    {
        fieldgroup(DropDown; MyField) { }
    }

    procedure GetRecordOnce(Force: Boolean)
    begin
    end;
}