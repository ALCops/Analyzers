interface IBase
{
    procedure DoSomething();
}

interface IDerived extends IBase
{
    procedure DoSomethingElse();
}

codeunit 50100 MyCodeunit implements IDerived
{
    Subtype = Test;

    [Test]
    procedure MyTestProcedure()
    begin
    end;

    procedure [|DoSomething()|]
    begin
    end;

    procedure [|DoSomethingElse()|]
    begin
    end;
}
