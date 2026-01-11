
tableextension 50100 MyCustomerExt extends Customer
{
    fields
    {
        [|field(50100; MyField; Integer) { }|]
    }
}

tableextension 50101 MyContactExt extends Contact
{
    fields
    {
        #pragma warning disable PC0020
        [|field(50100; MyField; Text[100]) { }|] // This field is enclosed in pragma to suppress the diagnostic on both fields.
        #pragma warning restore PC0020
    }
}

table 18 Customer { fields { field(1; "No."; Code[20]) { } } }
table 50100 Contact { fields { field(1; "No."; Code[20]) { } } }