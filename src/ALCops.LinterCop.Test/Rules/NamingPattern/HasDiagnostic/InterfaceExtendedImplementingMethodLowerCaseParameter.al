interface IBase
{
    procedure MyBaseMethod(Amount: Decimal);
}

interface IDerived extends IBase
{
}

codeunit 50100 MyCodeunit implements IDerived
{
    procedure MyBaseMethod([|netAmount|]: Decimal)
    begin
    end;
}
