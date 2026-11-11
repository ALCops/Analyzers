table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
    }
}

codeunit 50101 MyCodeunit
{
    procedure [|Build|](Txt: Text) Result: Record MyTable
    begin
        Evaluate(Result."Entry No.", Txt);
    end;

    procedure [|BuildInCondition|](Txt: Text) Result: Record MyTable
    begin
        if not Evaluate(Result."Entry No.", Txt) then begin
            Error('Invalid entry number.');
        end;
    end;
}
