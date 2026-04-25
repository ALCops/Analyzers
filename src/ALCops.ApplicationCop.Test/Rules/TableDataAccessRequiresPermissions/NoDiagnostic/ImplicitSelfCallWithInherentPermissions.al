table 50000 MyTable
{
    Caption = '', Locked = true;
    InherentPermissions = rimd;

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }

    procedure DoSomething()
    begin
        [|Rec.Modify();|]
        [|Rec.FindFirst();|]
        [|Rec.Insert();|]
        [|Rec.Delete();|]
    end;
}
