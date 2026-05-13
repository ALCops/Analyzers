report 50000 MyReport
{
    Permissions = [|tabledata MyTable = rimd|];

    dataset
    {
        dataitem(MyTable; MyTable)
        {
            trigger OnAfterGetRecord()
            begin
                MyTable.Modify();
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
