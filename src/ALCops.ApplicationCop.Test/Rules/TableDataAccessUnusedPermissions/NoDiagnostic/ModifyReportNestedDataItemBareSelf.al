report 50000 MyReport
{
    Permissions = [|tabledata Inner = rm|],
                  [|tabledata Outer = r|];

    dataset
    {
        dataitem(Outer; Outer)
        {
            dataitem(Inner; Inner)
            {
                trigger OnAfterGetRecord()
                begin
                    Modify();
                end;
            }
        }
    }
}

table 50000 Outer
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

table 50001 Inner
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
