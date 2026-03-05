codeunit 50100 MyCodeunit
{
    Subtype = Test;

    [Test]
    procedure MyTestProcedure()
    begin
    end;

    internal procedure [|MyHelperProcedure|]()
    begin
    end;
}
