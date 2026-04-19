interface IMyInterface
{
    procedure myInterfaceMethod();
}

codeunit 50100 MyCodeunit implements IMyInterface
{
    [|procedure myInterfaceMethod()|]
    begin
    end;
}
