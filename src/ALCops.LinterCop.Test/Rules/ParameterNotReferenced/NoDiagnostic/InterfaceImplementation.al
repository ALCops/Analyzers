codeunit 50102 MyCodeunit implements MyInterface
{
    procedure MyProcedure([|MyInteger: Integer|]; MyText: Text; MyDate: Date)
    begin
        MyText := 'Hello';
        MyDate := Today();
    end;
}

interface MyInterface
{
    procedure MyProcedure(MyInteger: Integer; MyText: Text; MyDate: Date);
}
