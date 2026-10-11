report 50000 MyReport
{
    Permissions = [|tabledata MyTable = m|];

    dataset
    {
        dataitem(MyTable; MyTable)
        {
            UseTemporary = true;

            trigger OnAfterGetRecord()
            begin
                Modify();
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
