codeunit 50100 MyCodeunit
{
    procedure Caller()
    var
        MySelf: Codeunit MyCodeunit;
    begin
        MySelf.MyProcedure();
    end;

    internal procedure [|MyProcedure|]()
    begin
    end;
}