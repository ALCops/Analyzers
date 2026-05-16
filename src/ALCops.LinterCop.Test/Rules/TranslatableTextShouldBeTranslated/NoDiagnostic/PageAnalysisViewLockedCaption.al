page 50100 MyPage
{
    Caption = 'My Page', Locked = true;
    PageType = List;

    analysisviews
    {
        analysisview(MyAnalysisView)
        {
            [|Caption|] = 'My Analysis View', Locked = true;
            DefinitionFile = 'MyAnalysisView.analysis.json';
        }
    }
}
