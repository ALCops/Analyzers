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
        case Input of
            1:
                exit(10);
            2:
                exit(20);
            else
                Rec.FieldError(Rec.Description, 'unsupported');
        end;
    end;
}