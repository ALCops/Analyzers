pageextension 50100 MyHeadlineExtension extends HeadlinePartPage
{
    layout
    {
        addlast(Content)
        {
            field([|MyExtensionHeadlineField|]; MyExtensionHeadlineText)
            {
                ApplicationArea = All;
            }
        }
    }

    var
        MyExtensionHeadlineText: Text;
}

page 50100 HeadlinePartPage
{
    PageType = HeadlinePart;
    Caption = 'Headline';

    layout
    {
        area(Content)
        {
        }
    }
}
