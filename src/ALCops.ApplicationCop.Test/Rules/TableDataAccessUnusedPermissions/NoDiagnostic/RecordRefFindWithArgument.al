codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = r|];

    local procedure GetDefaultLocationCode(TableId: Integer; AccountNo: Code[20]) LocationCode: Code[10]
    var
        RecordRef: RecordRef;
        FieldRef: FieldRef;
    begin
        RecordRef.Open(TableId);
        FieldRef := RecordRef.Field(1);
        FieldRef.Value(AccountNo);
        RecordRef.Find('=');
        Evaluate(LocationCode, Format(FieldRef.Value()));
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
