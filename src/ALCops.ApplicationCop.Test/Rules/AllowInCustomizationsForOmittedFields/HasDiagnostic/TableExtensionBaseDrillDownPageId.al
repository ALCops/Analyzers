tableextension 50101 MyTable extends MyTable
{
    fields
    {
        field(2; [|MyField|]; Integer) { }
    }
}

table 50100 MyTable
{
    DrillDownPageId = MyPage;
    fields { field(1; "Primary Key"; Code[10]) { } }
}

page 50000 MyPage { }