codeunit 50100 "FieldError Spacing Valid"
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

    procedure UserMethodsAreNotScopeLeaving(Handler: Codeunit "Error Method Handler")
    begin
        Message('Start');
        [|Handler|].Error();
        [|Handler|].FieldError();
    end;
}

codeunit 50101 "Error Method Handler"
{
    procedure Error()
    begin
    end;

    procedure FieldError()
    begin
    end;
}

table 50102 "FieldError Table"
{
    fields
    {
        field(1; Value; Integer)
        {
        }
    }
}