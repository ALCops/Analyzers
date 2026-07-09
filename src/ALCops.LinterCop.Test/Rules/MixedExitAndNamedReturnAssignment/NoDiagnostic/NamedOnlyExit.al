codeunit 50100 MyCodeunit
{
    procedure Compute(): Integer
    begin
        [|exit(1);|]
    end;
}