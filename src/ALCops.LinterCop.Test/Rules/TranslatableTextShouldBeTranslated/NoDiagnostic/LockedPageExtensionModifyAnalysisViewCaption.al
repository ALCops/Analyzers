pageextension 50100 MyPageExt extends MyPage
{
    analysisviews
    {
        modify(MyAnalysisView)
        {
            [|Caption = 'My Modified Analysis View', Locked = true|];
        }
    }
}

page 50100 MyPage
{
    PageType = List;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            repeater(MyRepeater)
            {
                field(MyField; Rec.MyField) { }
            }
        }
    }

    analysisviews
    {
        analysisview(MyAnalysisView)
        {
            DefinitionFile = 'MyAnalysisView.analysis.json';
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
