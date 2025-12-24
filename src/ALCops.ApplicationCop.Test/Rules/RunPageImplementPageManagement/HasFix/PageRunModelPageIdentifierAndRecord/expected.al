codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        SalesHeader: Record "Sales Header";
        PageManagement: Codeunit "Page Management";
    begin
        PageManagement.PageRunModal(SalesHeader);
    end;
}

page 50100 MyPage { }
table 36 "Sales Header"
{
    fields { field(1; MyField; Integer) { } }
}
