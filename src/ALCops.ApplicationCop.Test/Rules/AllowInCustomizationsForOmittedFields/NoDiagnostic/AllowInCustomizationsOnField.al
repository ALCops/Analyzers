table 50100 MyTable
{
    fields
    {
        field(1; [|MyField|]; Integer)
        {
            AllowInCustomizations = Never;
        }
    }
}

page 50100 MyPage { SourceTable = MyTable; }
