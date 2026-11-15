table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

page 50100 MyPage
{
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field(MyField; Rec.MyField) { }
        }
    }
}

report 50100 MyReport
{
    ProcessingOnly = true;

    trigger OnPreReport()
    begin
    end;
}

enum 50100 MyEnum
{
    Extensible = true;

    value(0; None) { }
}

permissionset 50100 MyPermissionSet
{
    Permissions = tabledata MyTable = R;
}

tableextension 50100 [|MyTableExt|] extends MyTable
{
}

pageextension 50100 [|MyPageExt|] extends MyPage
{
}

reportextension 50100 [|MyReportExt|] extends MyReport
{
}

enumextension 50100 [|MyEnumExt|] extends MyEnum
{
}

permissionsetextension 50100 [|MyPermissionSetExt|] extends MyPermissionSet
{
}
