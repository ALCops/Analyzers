codeunit 50100 MyCodeunit
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
