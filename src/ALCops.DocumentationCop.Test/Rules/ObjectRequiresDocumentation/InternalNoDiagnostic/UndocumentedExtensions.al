/// <summary>
/// Stores the sample records.
/// </summary>
table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

/// <summary>
/// Shows the sample records.
/// </summary>
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

/// <summary>
/// Processes the sample records.
/// </summary>
report 50100 MyReport
{
    ProcessingOnly = true;

    trigger OnPreReport()
    begin
    end;
}

/// <summary>
/// Lists the sample options.
/// </summary>
enum 50100 MyEnum
{
    Extensible = true;

    value(0; None) { }
}

/// <summary>
/// Grants read access to the sample records.
/// </summary>
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