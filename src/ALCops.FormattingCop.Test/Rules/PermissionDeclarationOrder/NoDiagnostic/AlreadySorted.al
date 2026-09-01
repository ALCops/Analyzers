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

table 50101 Bravo
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
    [|Permissions = table Alpha = X,
                  tabledata Alpha = R,
                  tabledata Bravo = R,
                  codeunit "My Codeunit" = X,
                  page "My Page" = X,
                  query "My Query" = X,
                  report "My Report" = X,
                  xmlport "My XmlPort" = X|];
}
