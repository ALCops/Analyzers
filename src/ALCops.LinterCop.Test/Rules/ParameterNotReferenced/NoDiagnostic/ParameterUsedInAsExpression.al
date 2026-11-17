interface IMyInterface
{
    procedure DoWork();
}

interface IOther
{
    procedure DoWork();
}

codeunit 50100 ParamUsedInAsExpression
{
    procedure MyProcedure([|Obj: Interface IMyInterface|])
    begin
        (Obj as IOther).DoWork();
    end;
}
