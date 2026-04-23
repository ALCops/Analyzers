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
            [|Caption|] = 'This is a caption text exactly at the maximum allowed length of two hundred and fifty characters in the Business Central Report Layout Selection page so it should not cause any runtime error when a user opens or selects this specific report layout ok';
            Type = Word;
            LayoutFile = 'MyReport.docx';
        }
    }
}
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
