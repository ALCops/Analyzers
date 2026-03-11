tableextension 50100 MyTable extends MyTable
{
    fields
    {
        field(2; [|MyField|]; Integer) { }
    }
}

table 50100 MyTable
{
    AllowInCustomizations = Never;

    fields
    {
        field(1; "Primary Key"; Code[10]) { }
    }
}

page 50100 MyPage { SourceTable = MyTable; }
