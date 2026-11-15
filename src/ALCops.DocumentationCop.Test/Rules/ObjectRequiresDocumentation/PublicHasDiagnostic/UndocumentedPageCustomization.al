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

pagecustomization [|MyPageCustomization|] customizes MyPage
{
}