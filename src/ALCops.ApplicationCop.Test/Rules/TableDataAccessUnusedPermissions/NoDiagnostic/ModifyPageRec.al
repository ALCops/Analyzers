report 50000 MyReport
{
    ProcessingOnly = true;
    Permissions = [|tabledata MyTable = m|];

    requestpage
    {
        SourceTable = MyTable;

        trigger OnOpenPage()
        begin
            Rec.Modify();
        end;
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
