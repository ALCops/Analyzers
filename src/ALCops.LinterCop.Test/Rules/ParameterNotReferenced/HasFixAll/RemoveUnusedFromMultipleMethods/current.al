codeunit 50100 FixAllMultipleMethods
{
    procedure FirstProcedure(MyInteger: Integer; [|UnusedA: Text|])
    begin
        MyInteger := 1;
    end;

    procedure SecondProcedure([|UnusedB: Date|]; MyText: Text)
    begin
        MyText := 'x';
    end;
}
