table 50100 MyTable
{
    fields
    {
        field(1; MyOption; Option)
        {
            OptionMembers = First,Second;
            [|OptionCaption = 'First,Second', Locked = true|];
        }
    }
}
