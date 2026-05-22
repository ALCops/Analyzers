xmlport 50000 MyXmlport
{
    Permissions = [|tabledata Alpha = rim|],
                  [|tabledata Beta = rim|],
                  [|tabledata Charlie = rim|],
                  [|tabledata Delta = rim|];

    schema
    {
        tableelement(Alpha; Alpha)
        {
            tableelement(Beta; Beta)
            {
                tableelement(Charlie; Charlie)
                {
                    tableelement(Delta; Delta)
                    {
                        fieldelement(DeltaField; Delta.MyField)
                        {
                        }
                    }

                    trigger OnBeforeInsertRecord()
                    begin
                        Charlie.Modify();
                    end;
                }

                trigger OnBeforeInsertRecord()
                begin
                    Beta.Modify();
                end;
            }

            trigger OnBeforeInsertRecord()
            begin
                Alpha.Modify();
            end;
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

table 50003 Delta
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
