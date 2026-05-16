page 50100 MyPage
{
    PageType = List;

    analysisviews
    {
        analysisview(MyAnalysisView)
        {
            ToolTip = [|'Specifies the analysis view that is used to display data in a way that helps you analyze trends and patterns. This is a very long tooltip that exceeds the maximum allowed length of 200 characters to test the maximum length rule.'|];
            DefinitionFile = 'MyAnalysisView.analysis.json';
        }
    }
}
