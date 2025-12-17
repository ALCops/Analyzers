permissionset 50100 MyPermissionSet
{
    Permissions =
        codeunit [|50100|] = X,
        page [|50100|] = X,
        table [|50100|] = X,
        report [|50100|] = X,
        xmlport [|50100|] = X,
        query [|50100|] = X;
}

codeunit 50100 MyCodeunit { }
page 50100 MyPage { }
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
report 50100 MyReport { }
xmlport 50100 MyXmlport { }
query 50100 MyQuery { elements { dataitem(MyDataItem; MyTable) { column(MyColumn; MyField) { } } } }