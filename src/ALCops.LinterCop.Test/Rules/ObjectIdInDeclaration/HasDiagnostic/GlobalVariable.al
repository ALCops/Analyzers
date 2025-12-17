codeunit 50101 "My Codeunit"
{
    var
        MyCodeunit: Codeunit [|50100|];
        MyPage: Page [|50100|];
        MyTable: Record [|50100|];
        MyReport: Report [|50100|];
        MyXmlport: Xmlport [|50100|];
        MyQuery: Query [|50100|];
}

codeunit 50100 MyCodeunit { }
page 50100 MyPage { }
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
report 50100 MyReport { }
xmlport 50100 MyXmlport { }
query 50100 MyQuery { elements { dataitem(MyDataItem; MyTable) { column(MyColumn; MyField) { } } } }