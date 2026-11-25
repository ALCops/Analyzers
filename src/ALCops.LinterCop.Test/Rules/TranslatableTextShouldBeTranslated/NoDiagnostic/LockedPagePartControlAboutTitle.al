page 50100 MyPage
{
    layout
    {
        area(Content)
        {
            part(MyPart; MyOtherPage)
            {
                [|AboutTitle = 'About my part', Locked = true|];
            }
        }
    }
}

page 50101 MyOtherPage
{
    PageType = CardPart;
}
