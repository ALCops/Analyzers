codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyInteger: Integer; [|MyText: Text|]; MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;
}
