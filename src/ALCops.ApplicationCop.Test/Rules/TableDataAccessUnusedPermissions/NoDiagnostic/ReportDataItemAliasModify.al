report 50000 MyReport
{
    Permissions = [|tabledata MyTable = rm|];

    dataset
    {
        dataitem("My Table"; MyTable)
        {
            trigger OnAfterGetRecord()
            begin
                "My Table".Modify();
            end;
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
