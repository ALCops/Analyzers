pageextension 50100 MyPageExtension extends MyPage
{
    procedure MyProcedure()
    begin
        if [|Confirm('Are You Sure?')|] then;
    end;
}

page 50100 MyPage
{
}
