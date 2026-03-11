table 50100 MyTable
{
    AllowInCustomizations = Never;

    fields
    {
        field(1; [|MyField|]; Integer) { }
    }
}

page 50100 MyPage { SourceTable = MyTable; }
