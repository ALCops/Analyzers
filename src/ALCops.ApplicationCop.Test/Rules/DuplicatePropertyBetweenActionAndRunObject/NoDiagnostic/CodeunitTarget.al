page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(PostDocuments)
            {
                [|Caption = 'MyPostingCodeunit';|]
                RunObject = codeunit MyPostingCodeunit;
            }
        }
    }
}

codeunit 50100 MyPostingCodeunit
{
}
