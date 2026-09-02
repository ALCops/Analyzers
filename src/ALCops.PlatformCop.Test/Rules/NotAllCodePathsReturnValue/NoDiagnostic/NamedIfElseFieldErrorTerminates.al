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
    procedure [|Compute|](Input: Integer) Result: Integer
    var
        Rec: Record MyTable;
    begin
        if Input = 1 then
            Result := 10
        else
            Rec.FieldError(Rec.Description);
    end;
}