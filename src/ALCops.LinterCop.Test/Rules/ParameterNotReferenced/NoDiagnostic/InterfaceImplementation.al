codeunit 50102 InterfaceImplementation implements InterfaceParameterContract
{
    procedure MyProcedure([|MyInteger: Integer|]; MyText: Text; MyDate: Date)
    begin
        MyText := 'Hello';
        MyDate := Today();
    end;
}

interface InterfaceParameterContract
{
    procedure MyProcedure(MyInteger: Integer; MyText: Text; MyDate: Date);
}
