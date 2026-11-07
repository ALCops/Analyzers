report 50000 MyReport
{
    Permissions = [|tabledata MyTempTable = m|];

    dataset
    {
        dataitem(MyTempTable; MyTempTable)
        {
            trigger OnAfterGetRecord()
            begin
                Modify();
            end;
        }
    }
}

table 50000 MyTempTable
{
    Caption = '', Locked = true;
    TableType = Temporary;

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
