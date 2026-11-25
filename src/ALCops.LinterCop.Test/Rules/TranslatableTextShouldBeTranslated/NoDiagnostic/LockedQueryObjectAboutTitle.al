query 50100 MyQuery
{
    QueryType = API;
    APIPublisher = 'myPublisher';
    APIGroup = 'myGroup';
    APIVersion = 'v1.0';
    EntityName = 'myQueryEntity';
    EntitySetName = 'myQueryEntities';
    [|AboutTitle = 'About my query', Locked = true|];

    elements
    {
        dataitem(MyTable; MyTable)
        {
            column(MyField; MyField) { }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
