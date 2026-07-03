xmlport 50000 MyXmlport
{
    Direction = Both;

    schema
    {
        textelement(NodeName1)
        {
            [|tableelement(NodeName2; MyTable)|]
            {
                UseTemporary = true;

                fieldattribute(NodeName3; NodeName2.MyField2)
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
        field(2; MyField2; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
