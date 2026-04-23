report 50100 MyReport
{
    DefaultRenderingLayout = MyLayout;

    dataset
    {
        dataitem(MyTable; MyTable)
        {
            column(MyField; MyField) { }
        }
    }

    rendering
    {
        layout(MyLayout)
        {
            Caption = 'My Report Layout';
            [|Summary|] = 'A short description of my report layout';
            Type = Word;
            LayoutFile = 'MyReport.docx';
        }
    }
}
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
