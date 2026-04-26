report 50000 MyReport
{
    Permissions = [|tabledata MyTable = r|];

    dataset
    {
        dataitem(MyTable; MyTable)
        {
        }
    }
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
