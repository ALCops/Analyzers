table 50101 MyTable
{
    [|LookupPageId = MyPage|];

    fields { field(1; MyField; Integer) { } }
}

page 50100 MyPage { SourceTable = MyTable; }
