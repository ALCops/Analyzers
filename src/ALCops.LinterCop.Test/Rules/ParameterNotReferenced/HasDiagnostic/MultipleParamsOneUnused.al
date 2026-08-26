codeunit 50100 OneUnusedParameter
{
    procedure MyProcedure(MyInteger: Integer; [|MyText: Text|])
    begin
        MyInteger := 1;
    end;
}
