page 50100 "My Page"
{
    ApplicationArea = All;
    UsageCategory = Lists;
    [|AccessByPermission = tabledata Alpha = R|];
}

report 50101 "My Report"
{
    ApplicationArea = All;
    UsageCategory = ReportsAndAnalysis;
    [|AccessByPermission = tabledata Alpha = RIMD|];
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
