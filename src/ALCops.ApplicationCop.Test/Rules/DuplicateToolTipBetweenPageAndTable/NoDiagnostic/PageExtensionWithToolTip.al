pageextension 50000 MyPageExtension extends MyPage
{
    layout
    {
        addlast(Content)
        {
            field(NewField; Rec.MyField)
            {
                [|ToolTip = 'MyOtherToolTip'|];
            }
        }
    }
}

page 50100 MyPage { SourceTable = MyTable; }
table 50100 MyTable { fields { field(1; MyField; Code[20]) { ToolTip = 'MyToolTip'; } } }