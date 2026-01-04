table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Code[10]) { }
        field(2; [|MyField|]; Integer)
        {
            ObsoleteState = Pending;
        }
    }
}

page 50000 MyPage { SourceTable = MyTable; }