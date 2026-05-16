page 50100 MyPage
{
    Caption = 'My Page';
    PageType = List;

    analysisviews
    {
        analysisview(MyAnalysisView)
        {
            [|Caption|] = 'My Analysis View';
            DefinitionFile = 'MyAnalysisView.analysis.json';
        }
    }
}
