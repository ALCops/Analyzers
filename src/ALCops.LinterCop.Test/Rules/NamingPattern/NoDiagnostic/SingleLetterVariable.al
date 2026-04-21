codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        [|i|]: Integer;
        [|t|]: Text;
        [|x|]: Decimal;
    begin
        for i := 0 to 1 do
            t := Format(x);
    end;
}
