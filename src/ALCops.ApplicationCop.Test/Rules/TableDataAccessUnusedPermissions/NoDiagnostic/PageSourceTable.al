page 50000 MyPage
{
    SourceTable = MyTable;
    Permissions = [|tabledata MyTable = rimd|];

    layout
    {
        area(Content)
        {
            field(MyField; Rec.MyField)
            {
            }
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
