table 50100 MyTable
{
    AllowInCustomizations = Never;

    fields
    {
        field(1; MyField; Integer)
        {
        }
    }

    keys
    {
        key(Key1; MyField)
        {
            Clustered = true;
        }
    }
}
