table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }

    procedure DoSomething(Other: Record MyTable)
    begin
    end;
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
                field(Name; Rec.Name)
                {
                    trigger OnValidate()
                    begin
                        Rec.DoSomething([|xRec|]);
                    end;
                }
            }
        }
    }
}
