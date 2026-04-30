codeunit 50100 MyCodeunit
{
    procedure MyProcedure([|MyInteger: Integer|])
    var
        Result: Integer;
    begin
        Result := MyInteger + 1;
    end;
}
