codeunit 50100 "FieldError Guard Clauses"
{
    procedure [|CheckValues|](Rec: Record "Guard Clause Table"; AnyField: FieldRef)
    begin
        if true then
            Rec.FieldError(Rec.Value);
        if true then
            AnyField.FieldError('Invalid value.');
        if true then
            Rec.FieldError(Rec.Value);
        if true then
            AnyField.FieldError('Invalid value.');
        if true then
            Rec.FieldError(Rec.Value);
        if true then
            AnyField.FieldError('Invalid value.');
        if true then
            Rec.FieldError(Rec.Value);
        if true then
            AnyField.FieldError('Invalid value.');
        if true then
            Rec.FieldError(Rec.Value);
        if true then
            AnyField.FieldError('Invalid value.');
        if true then
            Rec.FieldError(Rec.Value);
        if true then
            AnyField.FieldError('Invalid value.');
        if true then
            Rec.FieldError(Rec.Value);
        if true then
            AnyField.FieldError('Invalid value.');
        if true then
            Rec.FieldError(Rec.Value);
    end;
}

table 50101 "Guard Clause Table"
{
    fields
    {
        field(1; Value; Integer)
        {
        }
    }
}