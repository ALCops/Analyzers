interface IMyInterface
{
    procedure [|myAbstractMethod|]([|myParam|]: Integer) [|myResult|]: Integer;

    procedure [|myDefaultMethod|]([|myParam|]: Integer) [|myResult|]: Integer
    begin
        exit(myParam);
    end;
}
