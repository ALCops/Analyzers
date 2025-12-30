codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        myKeyValue: Text;
        mySecret: Text;
    begin
        IsolatedStorage.Get(myKeyValue, [|mySecret|]);
        IsolatedStorage.Get(myKeyValue, DataScope::Module, [|mySecret|]);
        IsolatedStorage.Set(myKeyValue, [|mySecret|]);
        IsolatedStorage.Set(myKeyValue, [|mySecret|], DataScope::Company);
    end;
}