codeunit 50100 MyCodeunit
{
    procedure Compute() Result: Integer
    begin
        [|Result := 1;|]
    end;
}