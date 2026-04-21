page 50100 MyAPIPage
{
    PageType = API;
    APIPublisher = 'myAPIPublisher';
    APIGroup = 'myAPIGroup';
    APIVersion = 'v1.0';
    EntityName = 'myEntityName';
    EntitySetName = 'myEntitySetName';
    SourceTable = MyTable;
    DelayedInsert = true;

    layout
    {
        area(Content)
        {
            group(Records)
            {
                field([|id|]; Rec.SystemId) { }
            }
        }
    }
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
