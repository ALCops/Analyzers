table 50000 MyTable
{
    Permissions = [|tabledata MyTable = rim|];

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }

    procedure SelfAccess()
    begin
        this.FindFirst();
        this.Insert();
        this.Modify();
    end;
}
