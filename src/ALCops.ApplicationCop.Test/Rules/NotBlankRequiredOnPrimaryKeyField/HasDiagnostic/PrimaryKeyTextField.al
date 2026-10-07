// Pins the Code-only scope of the setup-table heuristic: a Text "Primary Key" still reports.
table 50100 MyTable
{
    fields
    {
        [|field(1; "Primary Key"; Text[250])|]
        {
        }
    }

    keys
    {
        key(Key1; "Primary Key")
        {
            Clustered = true;
        }
    }
}
