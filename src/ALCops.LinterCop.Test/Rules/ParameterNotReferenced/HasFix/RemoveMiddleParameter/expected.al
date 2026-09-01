codeunit 50100 RemoveMiddleUnusedParameter
{
    procedure MyProcedure(MyInteger: Integer; MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;
}
