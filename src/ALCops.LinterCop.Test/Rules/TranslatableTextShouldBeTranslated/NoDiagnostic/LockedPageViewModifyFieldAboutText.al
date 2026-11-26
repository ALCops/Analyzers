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
                field(MyViewField; Rec.MyField) { }
            }
        }
    }

    views
    {
        view(MyView)
        {
            SharedLayout = false;

            layout
            {
                modify(MyViewField)
                {
                    [|AboutText = 'My field in my view explains itself.', Locked = true|];
                }
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
