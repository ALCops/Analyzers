codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rimd|];

    internal procedure SumEntries(var MyRecord: Record MyTable): Integer
    var
        Total: Integer;
    begin
        repeat
            Total += MyRecord.MyField;
        until MyRecord.Next() = 0;
        exit(Total);
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
