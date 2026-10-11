table 50000 MyTable
{
    Caption = '', Locked = true;
    Permissions = [|tabledata MyTable = rd|];

    fields
    {
        field(1; MyKey; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }

    internal procedure Clear()
    begin
        DeleteAll;
    end;
}
