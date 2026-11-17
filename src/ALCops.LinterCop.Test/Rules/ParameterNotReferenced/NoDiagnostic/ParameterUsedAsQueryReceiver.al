table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

query 50100 MyQuery
{
    elements
    {
        dataitem(MyTable; MyTable)
        {
            column(Name; Name) { }
        }
    }
}

codeunit 50100 ParamUsedAsQueryReceiver
{
    procedure GetName(var [|MyQuery: Query MyQuery|]): Text
    begin
        exit(MyQuery.Name);
    end;
}
