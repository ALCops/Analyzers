permissionset 50000 MyPermissionSet
{
    Caption = '', Locked = true;
    Assignable = true;
    Permissions =
        [|tabledata MyTable = RIMD|],
        [|tabledata MyOtherTable = R|];
}

table 50000 MyTable
{
    Caption = '', Locked = true;

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}

table 50001 MyOtherTable
{
    Caption = '', Locked = true;

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
