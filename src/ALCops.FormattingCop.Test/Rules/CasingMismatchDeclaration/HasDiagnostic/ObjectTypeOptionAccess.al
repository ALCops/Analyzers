codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): ObjectType
    begin
        exit(ObjectType::[|xmlport|]);
    end;
}
