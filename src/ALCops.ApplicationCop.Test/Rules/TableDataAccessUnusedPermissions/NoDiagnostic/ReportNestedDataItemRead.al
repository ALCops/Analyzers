report 50000 MyReport
{
    Permissions = [|tabledata Alpha = r|],
                  [|tabledata Beta = r|],
                  [|tabledata Charlie = r|];

    dataset
    {
        dataitem(Alpha; Alpha)
        {
            dataitem(Beta; Beta)
            {
                dataitem(Charlie; Charlie)
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

table 50002 Charlie
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
