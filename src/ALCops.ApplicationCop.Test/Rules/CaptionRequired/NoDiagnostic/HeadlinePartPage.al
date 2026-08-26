page 50100 HeadlinePartPage
{
    PageType = HeadlinePart;
    Caption = 'Headline';

    layout
    {
        area(Content)
        {
            field([|MyHeadlineField|]; MyHeadlineText)
            {
                ApplicationArea = All;
                Visible = true;
            }
            group(MyGroup)
            {
                ShowCaption = false;
                field([|MyGroupedHeadlineField|]; MyHeadlineText)
                {
                    ApplicationArea = All;
                }
            }
        }
    }

    var
        MyHeadlineText: Text;
}
