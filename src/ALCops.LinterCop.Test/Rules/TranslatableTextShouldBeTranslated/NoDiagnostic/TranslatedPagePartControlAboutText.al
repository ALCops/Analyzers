page 50100 MyPage
{
    layout
    {
        area(Content)
        {
            part(MyPart; MyOtherPage)
            {
                [|AboutText = 'My part explains itself.'|];
            }
        }
    }
}

page 50101 MyOtherPage
{
    PageType = CardPart;
}
