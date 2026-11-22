interface "IMyInterface"
{
    procedure DoSomething();
    procedure Calculate(Value: Integer; var Result: Text): Decimal;
}

codeunit 50100 MyCodeunit implements "IMyInterface"
{
    Subtype = Test;

    [Test]
    procedure MyTestProcedure()
    begin
    end;

    procedure [|DoSomething()|]
    begin
    end;

    procedure [|Calculate(Value: Integer; var Result: Text)|]: Decimal
    begin
    end;
}
