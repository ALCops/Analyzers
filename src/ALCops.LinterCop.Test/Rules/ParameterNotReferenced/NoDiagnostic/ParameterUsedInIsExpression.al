interface IMyInterface
{
    procedure DoWork();
}

interface IOther
{
    procedure DoWork();
}

codeunit 50100 ParamUsedInIsExpression
{
    procedure MyProcedure([|Obj: Interface IMyInterface|]): Boolean
    begin
        exit(Obj is IOther);
    end;
}
