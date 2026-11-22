interface "IMyInterface"
{
    procedure Calculate(Value: Integer): Decimal;
}

codeunit 50100 MyCodeunit implements "IMyInterface"
{
    Subtype = Test;

    [Test]
    procedure MyTestProcedure()
    begin
    end;

    procedure Calculate(Value: Integer): Decimal
    begin
    end;

    procedure [|Calculate(Value: Text)|]: Decimal
    begin
    end;
}
