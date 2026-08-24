codeunit 50100 FixAllMultipleMethods
{
    procedure FirstProcedure(
        MyInteger: Integer)
    begin
        MyInteger := 1;
    end;

    procedure SecondProcedure(
        MyText: Text)
    begin
        MyText := 'x';
    end;

    procedure ThirdProcedure(MyInteger: Integer)
    begin
        MyInteger := 1;
    end;

    procedure FourthProcedure(MyText: Text)
    begin
        MyText := 'x';
    end;
}
