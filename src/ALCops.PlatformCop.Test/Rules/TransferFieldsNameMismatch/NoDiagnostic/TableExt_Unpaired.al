tableextension 50101 MyTableExt extends MyTable
{
    fields
    {
        [|field(50100; MyField2; Text[100]) { }|]
    }
}

table 50100 MyTable { fields { field(1; MyField; Code[20]) { } } }