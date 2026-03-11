tableextension 50100 MyTableExtension extends MyTable
{
    AllowInCustomizations = Never;

    fields
    {
        field(50100; MyExtField; Integer)
        {
            [|AllowInCustomizations = Never;|]
        }
    }
}

table 50100 MyTable { fields { field(1; "Primary Key"; Code[10]) { } } }
