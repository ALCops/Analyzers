pagecustomization MyPageCust customizes MyPage
{
    views
    {
        addlast
        {
            view(MyCustView)
            {
                [|Caption = 'My Customization View'|];
            }
        }
    }
}

page 50100 MyPage
{
    PageType = List;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            repeater(MyRepeater)
            {
                field(MyField; Rec.MyField) { }
            }
        }
    }

    views
    {
        view(MyView) { }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
        field(2; MyOption; Option)
        {
            OptionMembers = First,Second;
        }
    }
}
