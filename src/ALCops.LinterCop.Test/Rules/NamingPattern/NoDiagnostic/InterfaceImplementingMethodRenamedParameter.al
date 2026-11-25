interface IMyInterface
{
    procedure CalcAmount(Amount: Decimal): Decimal;
}

codeunit 50100 MyCodeunit implements IMyInterface
{
    procedure CalcAmount([|NetAmount|]: Decimal): Decimal
    begin
        exit(NetAmount);
    end;
}
