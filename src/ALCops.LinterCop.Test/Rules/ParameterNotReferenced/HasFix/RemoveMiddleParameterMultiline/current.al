codeunit 50100 RemoveMiddleMultiline
{
    procedure MyProcedure(
        MyInteger: Integer;
        // legacy parameter, no longer required
        [|MyText: Text|];
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;
}
