xmlport 50000 MyXmlPort
{
    Permissions = [|tabledata MyTable = rim|];

    schema
    {
        textelement(Root)
        {
            tableelement(MyTable; MyTable)
            {
                fieldelement(MyField; MyTable.MyField)
                {
                }

                trigger OnBeforeInsertRecord()
                begin
                    MyTable.Modify();
                end;
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
