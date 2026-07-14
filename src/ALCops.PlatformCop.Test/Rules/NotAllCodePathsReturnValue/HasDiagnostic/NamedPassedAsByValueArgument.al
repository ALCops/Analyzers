codeunit 50100 MyCodeunit
{
    procedure [|Compute|]() Result: Integer
    begin
        ReadOnly(Result);
    end;

    local procedure ReadOnly(Value: Integer)
    begin
        Message('%1', Value);
    end;
}
