table 50000 MyTempTable
{
    Permissions = [|tabledata MyTempTable = m|];
    TableType = Temporary;

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
