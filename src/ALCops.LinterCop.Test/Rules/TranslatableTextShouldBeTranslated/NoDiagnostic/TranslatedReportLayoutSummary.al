report 50100 MyReport
{
    DefaultRenderingLayout = MyLayout;

    dataset
    {
        dataitem(MyDataItem; MyTable)
        {
            column(MyField; MyField) { }
        }
    }

    rendering
    {
        layout(MyLayout)
        {
            Type = Excel;
            LayoutFile = 'MyLayout.xlsx';
            [|Summary = 'Summarizes my layout.'|];
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
