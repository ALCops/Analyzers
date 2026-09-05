namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50000 MyCodeunit
{
    var
        FullyQualified: Record MyPublisher.MyExtension.MyAppDomain.MyTable;
}

table 50000 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
