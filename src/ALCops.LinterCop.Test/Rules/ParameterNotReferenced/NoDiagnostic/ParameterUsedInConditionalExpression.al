codeunit 50100 ParamUsedInConditional
{
    procedure MyProcedure([|Flag: Boolean|]): Integer
    begin
        exit(Flag ? 1 : 2);
    end;
}
