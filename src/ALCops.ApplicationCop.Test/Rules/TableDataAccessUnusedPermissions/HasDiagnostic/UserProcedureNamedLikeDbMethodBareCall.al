codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = m|];

    procedure Modify()
    var
        MyRecord: Record MyTable;
    begin
        MyRecord.Init();
    end;

    procedure CallBare()
    begin
        Modify();
        Modify;
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
