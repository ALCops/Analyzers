interface IBase
{
    procedure MyBaseMethod();
}

interface IDerived extends IBase
{
    procedure MyDerivedMethod();
}

codeunit 50100 MyCodeunit implements IDerived
{
    procedure [|myBaseMethod|]()
    begin
    end;

    procedure MyDerivedMethod()
    begin
    end;
}
