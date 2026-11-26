page 50100 MyPage
{
    PageType = List;
    SourceTable = MyTable;

    views
    {
        view(MyView)
        {
            [|Caption = 'My View', Locked = true|];
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
