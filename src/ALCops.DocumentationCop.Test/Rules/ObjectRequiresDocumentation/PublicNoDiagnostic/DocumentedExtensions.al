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

/// <summary>
/// Adds fields to the sample table.
/// </summary>
tableextension 50100 [|MyTableExt|] extends MyTable
{
}

/// <summary>
/// Adds controls to the sample page.
/// </summary>
pageextension 50100 [|MyPageExt|] extends MyPage
{
}

/// <summary>
/// Adds logic to the sample report.
/// </summary>
reportextension 50100 [|MyReportExt|] extends MyReport
{
}

/// <summary>
/// Adds options to the sample enum.
/// </summary>
enumextension 50100 [|MyEnumExt|] extends MyEnum
{
}

/// <summary>
/// Adds permissions to the sample permission set.
/// </summary>
permissionsetextension 50100 [|MyPermissionSetExt|] extends MyPermissionSet
{
}