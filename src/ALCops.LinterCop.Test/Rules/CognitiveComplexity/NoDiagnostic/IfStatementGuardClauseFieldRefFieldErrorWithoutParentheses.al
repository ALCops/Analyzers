table 50100 MyTable
{
    fields
    {
        field(1; Description; Text[50]) { }
    }
}

codeunit 50100 MyCodeunit
{
    procedure [|MyProcedure|]() // Cognitive Complexity: 0 (threshold >=15)
    var
        RecRef: RecordRef;
        FldRef: FieldRef;
    begin
        RecRef.Open(Database::MyTable);
        FldRef := RecRef.Field(1);

        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
        if true then FldRef.FieldError; // +0 (nesting = 0)
    end;
}