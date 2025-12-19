codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    begin
        Page.Run(Page::MyPage, MyTable, 1);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
page 50100 MyPage { }