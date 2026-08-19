tableextension 50100 MyCustomerExt extends Customer
{
    fields
    {
        [|field(50100; "FOO My Field"; Integer) { }|] // Affix as prefix; stripped to "My Field"
    }
}

tableextension 50101 MyContactExt extends Contact
{
    fields
    {
        [|field(50100; "My Field FOO"; Integer) { }|] // Affix as suffix; stripped to "My Field"
    }
}

table 18 Customer { fields { field(1; "No."; Code[20]) { } } }
table 5050 Contact { fields { field(1; "No."; Code[20]) { } } }
