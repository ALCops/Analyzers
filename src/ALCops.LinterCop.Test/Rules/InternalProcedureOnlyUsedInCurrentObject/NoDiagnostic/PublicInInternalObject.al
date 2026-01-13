codeunit 50100 MyCodeunit
{
    Access = Internal;

    procedure Caller()
    begin
        MyProcedure();
    end;

    procedure [|MyProcedure|]()
    begin
    end;
}
