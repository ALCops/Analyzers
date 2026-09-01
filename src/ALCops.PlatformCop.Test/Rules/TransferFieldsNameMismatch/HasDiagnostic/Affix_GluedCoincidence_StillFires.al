tableextension 50100 MyCustomerExt extends Customer
{
    fields
    {
        // "Customer" coincidentally ends with the glued affix "MER", so it strips to "Custo".
        // The paired field's core ("Contact Ref") is genuinely different, so PC0021 still fires:
        // a coincidental strip must not silently swallow an unrelated name mismatch.
        [|field(50100; "Customer"; Integer) { }|]
    }
}

tableextension 50101 MyContactExt extends Contact
{
    fields
    {
        [|field(50100; "Contact Ref"; Integer) { }|]
    }
}

table 18 Customer { fields { field(1; "No."; Code[20]) { } } }
table 5050 Contact { fields { field(1; "No."; Code[20]) { } } }
