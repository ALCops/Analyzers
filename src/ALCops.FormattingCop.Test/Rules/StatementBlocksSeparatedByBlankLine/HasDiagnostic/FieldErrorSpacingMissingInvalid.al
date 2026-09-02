codeunit 50100 "Incomplete FieldError Spacing"
{
    procedure IncompleteFieldError(Rec: Record "FieldError Table")
    begin
        Message('Start');
        [|Rec|].FieldError(UndefinedField, 'Invalid value.');
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