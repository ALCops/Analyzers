query 50000 MyQuery
{
    Permissions = [|tabledata Alpha = r|],
                  [|tabledata Beta = r|];

    elements
    {
        dataitem(Alpha; Alpha)
        {
            column(AlphaField; MyField)
            {
            }

            dataitem(Beta; Beta)
            {
                DataItemLink = MyField = Alpha.MyField;

                column(BetaField; MyField)
                {
                }
            }
        }
    }
}

table 50000 Alpha
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

table 50001 Beta
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
