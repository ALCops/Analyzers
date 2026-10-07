table 50100 MySetup
{
    fields
    {
        [|field(1; PrimaryKey; Code[10])|]
        {
        }
        field(2; "Some Setting"; Boolean)
        {
        }
    }

    keys
    {
        key(Key1; PrimaryKey)
        {
            Clustered = true;
        }
    }
}
