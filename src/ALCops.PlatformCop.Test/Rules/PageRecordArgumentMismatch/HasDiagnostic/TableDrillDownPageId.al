table 50101 MyOtherTable
{
    [|DrillDownPageId = MyPage|];

    fields { field(1; MyField; Integer) { } }
}

page 50100 MyPage { SourceTable = MyTable; }
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
