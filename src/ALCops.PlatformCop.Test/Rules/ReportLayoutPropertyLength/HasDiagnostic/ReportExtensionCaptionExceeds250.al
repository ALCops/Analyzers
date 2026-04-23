reportextension 50101 MyReportExt extends MyReport
{
    rendering
    {
        layout(MyExtLayout)
        {
            [|Caption|] = 'This is a very long caption text that exceeds the maximum allowed length of two hundred and fifty characters in the Business Central Report Layout Selection page causing a runtime error when any user tries to open or select this specific report layout';
            Type = Word;
            LayoutFile = 'MyReportExt.docx';
        }
    }
}
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
            Type = Word;
            LayoutFile = 'MyReport.docx';
        }
    }
}
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
