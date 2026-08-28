codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = r|];

    trigger OnRun()
    var
        MyHelper: Codeunit MyHelper;
        MyRecord: Record MyTable;
        Total: Integer;
    begin
        if MyHelper.FindEntries(1, MyRecord) then
            repeat
                Total += MyRecord.MyField;
            until MyRecord.Next() = 0;
    end;
}

codeunit 50001 MyHelper
{
    Permissions = tabledata MyTable = r;

    internal procedure FindEntries(MyFieldValue: Integer; var MyRecord: Record MyTable): Boolean
    begin
        MyRecord.Reset();
        MyRecord.SetRange(MyField, MyFieldValue);
        exit(MyRecord.FindSet());
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
