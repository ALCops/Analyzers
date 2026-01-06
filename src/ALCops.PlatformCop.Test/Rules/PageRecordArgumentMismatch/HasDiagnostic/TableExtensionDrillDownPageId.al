tableextension 50100 MyOtherTableExtension extends MyOtherTable
{
    [|LookupPageId = MyPage|];
}

page 50100 MyPage { SourceTable = MyTable; }
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
table 50101 MyOtherTable { fields { field(1; MyField; Integer) { } } }