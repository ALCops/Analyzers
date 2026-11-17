codeunit 50100 ParamDifferentCasing
{
    procedure MyProcedure([|Name: Text|]): Text
    begin
        exit(name);
    end;
}
