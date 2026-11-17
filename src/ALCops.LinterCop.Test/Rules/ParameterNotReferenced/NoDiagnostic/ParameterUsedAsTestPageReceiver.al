table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

page 50100 MyCard
{
    PageType = Card;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field(Name; Rec.Name) { }
        }
    }
}

codeunit 50100 ParamUsedAsTestPage
{
    Subtype = Test;

    procedure SetNameFilter(var [|MyCard: TestPage MyCard|])
    begin
        MyCard.Filter.SetFilter(Name, 'x');
    end;
}
