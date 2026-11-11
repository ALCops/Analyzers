table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }
}

codeunit 50101 MyCodeunit
{
    procedure [|Build|](Condition: Boolean) Result: Record MyTable
    begin
        if Condition then begin
            Result."No." := 'A';
        end;
    end;
}
