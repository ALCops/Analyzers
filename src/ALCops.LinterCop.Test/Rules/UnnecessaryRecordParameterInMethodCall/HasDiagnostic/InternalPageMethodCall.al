table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

page 50100 MyPage
{
    PageType = Card;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            group(General)
            {
                field(Name; Rec.Name) { }
            }
        }
    }

    local procedure MyProcedure(var MyTableParam: Record MyTable)
    begin
    end;

    trigger OnOpenPage()
    begin
        MyProcedure([|Rec|]);
    end;
}
