codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        [|Page.Run(Page::MyPage, MyTable)|];
    end;
}

page 50100 MyPage { }
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }