
tableextension 50100 MyCustomerExt extends Customer
{
    fields
    {
        [|field(50100; MyField; Code[10]) { }|] // Same ID (50100) as in MyContactExt, same type but different length
    }
}

tableextension 50101 MyContactExt extends Contact
{
    fields
    {
        [|field(50100; MyField; Code[20]) { }|] // Same ID (50100) as in MyCustomerExt, same type but different length
    }
}

table 18 Customer { fields { field(1; "No."; Code[20]) { } } }
table 5050 Contact { fields { field(1; "No."; Code[20]) { } } }