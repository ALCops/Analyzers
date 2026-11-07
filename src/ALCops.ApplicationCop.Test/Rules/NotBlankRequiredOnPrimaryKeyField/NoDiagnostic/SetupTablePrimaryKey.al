table 50100 MySetup
{
    fields
    {
        [|field(1; "Primary Key"; Code[10])|]
        {
        }
        field(2; "Some Setting"; Boolean)
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
