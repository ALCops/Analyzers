table 50000 MyTempTable
{
    Caption = '', Locked = true;
    TableType = Temporary;

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
    end;
}
