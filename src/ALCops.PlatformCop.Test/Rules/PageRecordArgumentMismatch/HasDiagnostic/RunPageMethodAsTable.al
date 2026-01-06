
codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        [|Page.Run(Page::MyPage, GetMyOtherTable())|];
    end;

    local procedure GetMyOtherTable(): Record MyOtherTable
    begin
    end;
}

page 50100 MyPage { SourceTable = MyTable; }
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
table 50101 MyOtherTable { fields { field(1; MyField; Integer) { } } }