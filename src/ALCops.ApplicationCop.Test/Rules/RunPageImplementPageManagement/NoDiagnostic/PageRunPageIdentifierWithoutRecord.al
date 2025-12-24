codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        [|Page.Run(Page::MyPage);|]
    end;
}

page 50100 MyPage { }