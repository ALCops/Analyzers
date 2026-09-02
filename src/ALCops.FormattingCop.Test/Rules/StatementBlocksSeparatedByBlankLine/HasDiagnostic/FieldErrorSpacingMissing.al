codeunit 50100 "FieldError Spacing Missing"
{
    procedure RecordFieldError(Rec: Record "FieldError Table")
    begin
        Message('Start');
        [|Rec|].FieldError(Rec.Value, 'Invalid value.');
    end;

    procedure FieldRefFieldError(AnyField: FieldRef)
    begin
        Message('Start');
        [|AnyField|].FieldError('Invalid value.');
    end;
}

table 50101 "FieldError Table"
{
    fields
    {
        field(1; Value; Integer)
        {
        }
    }
}