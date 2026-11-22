interface "IMyInterface"
{
    procedure DoSomething();
}

codeunit 50100 MyCodeunit implements "IMyInterface"
{
    Subtype = Test;

    [Test]
    procedure MyTestProcedure()
    begin
    end;

    procedure DoSomething()
    begin
    end;

    procedure [|MyHelper()|]
    begin
    end;
}
