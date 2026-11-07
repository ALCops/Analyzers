namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = m|];

    procedure ModifyIt()
    var
        MyRecord: Record MyPublisher.MyExtension.MyAppDomain.MyTable;
    begin
        MyRecord.Modify();
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
