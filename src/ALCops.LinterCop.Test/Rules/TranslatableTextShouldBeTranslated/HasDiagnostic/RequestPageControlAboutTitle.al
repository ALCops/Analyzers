report 50100 MyReport
{
    ProcessingOnly = true;

    requestpage
    {
        layout
        {
            area(Content)
            {
                field(MyOption; MyOption)
                {
                    [|AboutTitle = 'About my option'|];
                }
            }
        }
    }

    var
        MyOption: Boolean;
}
