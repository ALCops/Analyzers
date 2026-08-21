codeunit 50102 InterfaceWrongCasing implements CaseInsensitiveContract
{
    procedure Myprocedure([|myInteger: Integer|]; Mytext: Text; MyDAte: Date)
    begin
        Mytext := 'Hello';
        MyDAte := Today();
    end;
}

interface CaseInsensitiveContract
{
    procedure MyProcedure(MyInteger: Integer; MyText: Text; MyDate: Date);
}
