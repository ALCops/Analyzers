
tableextension 50100 MyContactExt extends Contact
{
    fields
    {
        [|field(50100; MyField; Text[100]) { }|]
    }
}

table 50100 Contact { fields { field(1; "No."; Code[20]) { } } }
