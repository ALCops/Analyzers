codeunit 50100 MyCodeunit
{
    internal procedure [|MyProcedure|]()
    begin
    end;

    procedure Caller()
    var
        MyProcedure: Integer;
    begin
        MyProcedure := 1;
    end;
}