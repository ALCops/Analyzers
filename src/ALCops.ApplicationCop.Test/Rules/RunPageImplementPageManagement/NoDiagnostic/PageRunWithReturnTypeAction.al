codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        if [|Page.RunModal(Page::MyPage, MyTable)|] = Action::LookupOK then;
    end;
}

page 50100 MyPage { }
table 50100 MyTable
{
    fields { field(1; MyField; Integer) { } }
}