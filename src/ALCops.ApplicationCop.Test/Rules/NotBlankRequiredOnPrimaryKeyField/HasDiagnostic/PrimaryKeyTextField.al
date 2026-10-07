table 50100 MySetup
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
