interface IMyInterface
{
    procedure CalcAmount(NetAmount: Decimal) Result: Decimal;
}

codeunit 50100 MyCodeunit implements IMyInterface
{
    procedure CalcAmount([|netAmount|]: Decimal) [|result|]: Decimal
    begin
        exit(netAmount);
    end;
}
