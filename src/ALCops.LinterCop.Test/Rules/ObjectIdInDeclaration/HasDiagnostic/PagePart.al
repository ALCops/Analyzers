page 50101 "My Page"
{
    SourceTable = MyTable;

    layout
    {
        area(content)
        {
            part(MyPart; [|50100|]) { }
        }
    }
}

page 50100 MyPage
{
    PageType = CardPart;
    SourceTable = MyTable;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }