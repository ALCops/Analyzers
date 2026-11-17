codeunit 50100 ParamUsedUnderInvalidNode
{
    procedure MyProcedure(var [|MyRecord: Record UnknownTable|])
    begin
        MyRecord.Init();
    end;
}
