codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyGuid: Guid;
    begin
        if System.IsNullGuid(MyGuid) then;
    end;
}