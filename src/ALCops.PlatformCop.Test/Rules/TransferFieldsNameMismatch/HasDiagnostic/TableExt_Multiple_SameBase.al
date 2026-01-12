
tableextension 50100 MyCustomerExt extends Customer
{
    fields
    {
        field(50100; MyFieldA; Text[50]) { }
    }
}

tableextension 50101 MyCustomerExt2 extends Customer
{
    fields
    {
        [|field(50101; MyOtherFieldB; Integer) { }|] // Same ID (50101) as in MyContactExt2, different name
    }
}

tableextension 50102 MyContactExt extends Contact
{
    fields
    {
        [|field(50101; MyFieldB; Integer) { }|] // Same ID (50100) as in MyCustomerExt2, different name
    }
}

table 18 Customer { fields { field(1; "No."; Code[20]) { } } }
table 5050 Contact { fields { field(1; "No."; Code[20]) { } } }