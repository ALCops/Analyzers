codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        i: Integer;
    begin
        for i := 1 to 2 do
            continue;
    end;
}
