codeunit 50102 MyCodeunit implements MyInterface
{
    procedure Myprocedure([|myInteger: Integer|]; Mytext: Text; MyDAte: Date)
    begin
        Mytext := 'Hello';
        MyDAte := Today();
    end;
}

interface MyInterface
{
    procedure MyProcedure(MyInteger: Integer; MyText: Text; MyDate: Date);
}
