codeunit 50100 FixAllTwoUnusedParameters
{
    procedure MyProcedure(MyInteger: Integer; [|MyText: Text|]; [|MyDate: Date|])
    begin
        MyInteger := 1;
    end;
}
