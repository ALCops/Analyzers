codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyText: Text[100];
        MyDecimal: Decimal;
    begin
        [|MyText|] := 'Hello';
        [|MyDecimal|] := 42;
    end;
}
