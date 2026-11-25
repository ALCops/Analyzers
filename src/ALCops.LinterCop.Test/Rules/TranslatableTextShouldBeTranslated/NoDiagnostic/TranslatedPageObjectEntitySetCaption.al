page 50100 MyApiPage
{
    PageType = API;
    APIPublisher = 'myPublisher';
    APIGroup = 'myGroup';
    APIVersion = 'v1.0';
    EntityName = 'myEntity';
    EntitySetName = 'myEntities';
    [|EntitySetCaption = 'My Entities'|];
    SourceTable = MyTable;
    DelayedInsert = true;

    layout
    {
        area(Content)
        {
            field(myField; Rec.MyField) { }
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
