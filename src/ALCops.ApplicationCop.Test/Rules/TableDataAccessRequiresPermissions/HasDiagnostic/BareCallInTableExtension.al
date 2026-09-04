tableextension 50001 MyTableExtension extends MyTable
{
    fields
    {
        field(50000; ExtField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }

    procedure DoSomething()
    begin
        [|Modify();|]
    end;
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
