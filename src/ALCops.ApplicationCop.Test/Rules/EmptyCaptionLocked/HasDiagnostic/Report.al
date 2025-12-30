report 50100 MyReport
{
    [|Caption|] = '';
    DefaultRenderingLayout = MyLayout;

    dataset
    {
        dataitem(MyTable; MyTable)
        {
            column(MyField; MyField)
            {

            }
        }
    }

    requestpage
    {
        layout
        {
            area(Content)
            {
                group(MyGroup)
                {
                    [|Caption|] = '';

                    field(MyInt; myInt)
                    {
                        [|Caption|] = '';
                    }
                }
            }
        }

        actions
        {
            area(Processing)
            {
                action(MyAction)
                {
                    [|Caption|] = '';
                }
            }
        }
    }

    rendering
    {
        layout(MyLayout)
        {
            [|Caption|] = '';
            Type = Word;
            LayoutFile = 'MyReport.docx';
        }
    }

    var
        myInt: Integer;
}
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }