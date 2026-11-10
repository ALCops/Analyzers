table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
    }
}

codeunit 50101 MyCodeunit
{
    procedure [|Build|]() Result: Record MyTable
    begin
        Consume(Result."Entry No.");
    end;

    local procedure Consume(Value: Integer)
    begin
    end;
}
