page 50100 MyApiPage
{
    PageType = API;
    APIPublisher = 'MyPublisher';
    APIGroup = 'MyGroup';
    EntityName = 'MyEntityName';
    EntitySetName = 'MyEntitySetName';
    SourceTable = MyTable;
    DelayedInsert = true;
    ODataKeyFields = [|SystemRowVersion|];

    layout
    {
        area(Content)
        {
            repeater(Group)
            {
                field(SystemRowVersion; Rec.SystemRowVersion) { }
            }
        }
    }
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }