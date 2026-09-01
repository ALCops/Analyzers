codeunit 50100 MyCodeunit
{
    procedure [|Compute|]() Result: Integer
    begin
        ComputeInto(Result);
    end;

    local procedure ComputeInto(var Value: Integer)
    begin
        Value := 42;
    end;
}
