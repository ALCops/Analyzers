pageextension 50100 MyPageExt extends MyPage
{
    views
    {
        addlast
        {
            view(MyExtView)
            {
                SharedLayout = false;

                layout
                {
                    modify(MyExtViewField)
                    {
                        [|AboutTitle = 'About my field in my extension view', Locked = true|];
                    }
                }
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
                field(MyExtViewField; Rec.MyField) { }
            }
        }
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
