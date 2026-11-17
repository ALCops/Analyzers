table 50100 MyTableA
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
    }
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(50100; ExtField; Boolean) { }
    }
}
