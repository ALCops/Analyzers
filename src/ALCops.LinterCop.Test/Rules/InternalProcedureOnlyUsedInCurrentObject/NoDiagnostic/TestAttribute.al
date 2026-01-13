codeunit 50100 MyCodeunit
{
    Subtype = Test;

    procedure Caller()
    begin
        MyProcedure();
    end;

    [Test]
    internal procedure [|MyProcedure|]()
    begin
    end;
}
