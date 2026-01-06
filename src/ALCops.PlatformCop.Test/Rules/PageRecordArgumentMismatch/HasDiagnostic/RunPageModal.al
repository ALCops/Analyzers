codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyOtherTable: Record MyOtherTable;
    begin
        [|Page.RunModal(Page::MyPage, MyOtherTable)|];
    end;
}

page 50100 MyPage { SourceTable = MyTable; }
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
table 50101 MyOtherTable { fields { field(1; MyField; Integer) { } } }