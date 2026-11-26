pageextension 50100 MyPageExt extends MyPage
{
    views
    {
        addlast
        {
            view(MyExtView)
            {
                [|Caption = 'My Extension View', Locked = true|];
            }
        }
    }
}

page 50100 MyPage
{
    PageType = List;
    SourceTable = MyTable;
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
