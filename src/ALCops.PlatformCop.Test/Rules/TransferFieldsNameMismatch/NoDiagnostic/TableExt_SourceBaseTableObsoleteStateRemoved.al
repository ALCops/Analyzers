tableextension 50100 MyCustomerExt extends Customer
{
    fields
    {
        [|field(50100; FieldA; Integer) { }|]
    }
}

tableextension 50101 MyContactExt extends Contact
{
    fields
    {
        [|field(50100; FieldB; Integer) { }|]
    }
}

table 18 Customer
{
    ObsoleteState = Removed;

    fields { field(1; "No."; Code[20]) { } }
}

table 5050 Contact { fields { field(1; "No."; Code[20]) { } } }
