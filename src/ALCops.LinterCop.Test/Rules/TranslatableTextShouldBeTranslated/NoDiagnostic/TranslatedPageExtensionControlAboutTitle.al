pageextension 50100 MyPageExt extends MyPage
{
    layout
    {
        addlast(Content)
        {
            field(MyExtField; Rec.MyField)
            {
                [|AboutTitle = 'About my extension field'|];
            }
        }
    }
}

page 50100 MyPage
{
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
    }
}
