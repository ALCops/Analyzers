page 50100 MyPage
{
    Caption = 'My Page';
    PageType = List;

    analysisviews
    {
        analysisview(MyAnalysisView)
        {
            [|Caption|] = '';
            DefinitionFile = 'MyAnalysisView.analysis.json';
        }
    }
}
