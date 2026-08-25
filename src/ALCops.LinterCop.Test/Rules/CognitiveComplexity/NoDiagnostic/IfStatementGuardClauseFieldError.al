table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Description; Text[50]) { }
    }
}

codeunit 50100 MyCodeunit
{
    procedure [|MyProcedure|]() // Cognitive Complexity: 0 (threshold >=15)
    var
        Rec: Record MyTable;
    begin
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then                                       // +0 (nesting = 0)
            Rec.FieldError(Description, 'something went wrong');
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
        if true then Rec.FieldError(Description);          // +0 (nesting = 0)
    end;
}
