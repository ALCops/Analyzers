pagecustomization MyPageCust customizes MyPage
{
    layout
    {
        modify(MyField)
        {
            [|AboutTitle = 'About my modified field', Locked = true|];
        }
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
            group(MyGroup)
            {
                field(MyField; Rec.MyField) { }
                field(MyOption; Rec.MyOption) { }
            }
            part(MyPart; MyPartPage) { }
        }
    }

    actions
    {
        area(Processing)
        {
            group(MyActionGroup)
            {
                action(MyAction)
                {
                    trigger OnAction()
                    begin
                    end;
                }
            }
        }
    }
}

page 50101 MyPartPage
{
    PageType = CardPart;
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
