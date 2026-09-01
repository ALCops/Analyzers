codeunit 50101 "My Codeunit"
{
}

page 50100 "My Page"
{
}

query 50100 "My Query"
{
    elements
    {
        dataitem(Item; Alpha)
        {
            column(MyField; MyField) { }
        }
    }
}

report 50100 "My Report"
{
}

xmlport 50100 "My XmlPort"
{
    schema
    {
        textelement(Root) { }
    }
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

permissionset 50100 "My Permission Set"
{
    Assignable = true;
    [|Permissions = xmlport "My XmlPort" = X,
                  report "My Report" = X,
                  query "My Query" = X,
                  page "My Page" = X,
                  codeunit "My Codeunit" = X|];
}
