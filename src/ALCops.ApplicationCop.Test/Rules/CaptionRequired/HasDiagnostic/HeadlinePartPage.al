page 50100 [|HeadlinePartPage|]
{
    PageType = HeadlinePart;

    layout
    {
        area(Content)
        {
            field(MyHeadlineField; MyHeadlineText)
            {
                ApplicationArea = All;
            }
        }
    }

    actions
    {
        area(Processing)
        {
            action([|MyAction|])
            {
            }
        }
    }

    var
        MyHeadlineText: Text;
}
