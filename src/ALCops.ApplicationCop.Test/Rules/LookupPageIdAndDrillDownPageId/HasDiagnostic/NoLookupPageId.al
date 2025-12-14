[|table 50100 MyTable|]
{
    DrillDownPageId = MyPage;

    fields
    {
        field(1; MyField; Integer) { }
    }
}

page 50100 MyPage
{
    PageType = List;
    SourceTable = MyTable;
}