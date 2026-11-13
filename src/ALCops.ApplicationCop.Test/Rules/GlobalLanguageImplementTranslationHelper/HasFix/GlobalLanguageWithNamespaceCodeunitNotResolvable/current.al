namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        [|GlobalLanguage(1)|];
    end;
}