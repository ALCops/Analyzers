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
        RecRef: RecordRef;
        FldRef: FieldRef;
    begin
        RecRef.Open(Database::MyTable);
        FldRef := RecRef.Field(1);

        if Input = 1 then
            exit(10)
        else
            FldRef.FieldError('unsupported');
    end;
}