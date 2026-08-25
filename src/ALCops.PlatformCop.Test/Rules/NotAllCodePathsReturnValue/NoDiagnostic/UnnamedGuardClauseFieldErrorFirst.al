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
    procedure [|Compute|](Input: Integer): Integer
    var
        Rec: Record MyTable;
    begin
        if Input < 0 then
            Rec.FieldError(Rec.Description, 'negative not supported');

        exit(Input * 2);
    end;
}
