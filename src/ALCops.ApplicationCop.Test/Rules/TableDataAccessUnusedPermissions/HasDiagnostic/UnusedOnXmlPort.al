xmlport 50000 MyXmlPort
{
    Permissions = [|tabledata OtherTable = r|];

    schema
    {
        textelement(Root)
        {
            tableelement(MyTable; MyTable)
            {
                fieldelement(MyField; MyTable.MyField)
                {
                }
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

table 50001 OtherTable
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
