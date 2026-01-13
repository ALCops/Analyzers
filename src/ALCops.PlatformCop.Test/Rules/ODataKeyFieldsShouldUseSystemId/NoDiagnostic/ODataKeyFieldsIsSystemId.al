page 50100 MyApiPage
{
    PageType = API;
    APIPublisher = 'MyPublisher';
    APIGroup = 'MyGroup';
    EntityName = 'MyEntityName';
    EntitySetName = 'MyEntitySetName';
    SourceTable = MyTable;
    DelayedInsert = true;
    ODataKeyFields = [|SystemId|];

    layout
    {
        area(Content)
        {
            repeater(Group)
            {
                field(SystemId; Rec.SystemId) { }
            }
        }
    }
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }