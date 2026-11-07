table 50000 MyTable
{
    Permissions = [|tabledata MyTable = m|];

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }

    procedure ModifySelf()
    begin
        Modify;
    end;
}
