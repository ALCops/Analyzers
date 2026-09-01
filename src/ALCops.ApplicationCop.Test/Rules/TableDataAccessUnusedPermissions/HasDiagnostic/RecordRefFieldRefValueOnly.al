codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = r|];

    local procedure ValidateInput(RecRelatedVariant: Variant; FieldNumber: Integer)
    var
        RecRef: RecordRef;
        FldRef: FieldRef;
        DecimalValue: Decimal;
    begin
        GetRecordRef(RecRelatedVariant, RecRef);
        FldRef := RecRef.Field(FieldNumber);
        if not Evaluate(DecimalValue, Format(FldRef.Value())) then
            DecimalValue := 0;
    end;

    local procedure GetRecordRef(RecRelatedVariant: Variant; var RecRef: RecordRef)
    begin
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
