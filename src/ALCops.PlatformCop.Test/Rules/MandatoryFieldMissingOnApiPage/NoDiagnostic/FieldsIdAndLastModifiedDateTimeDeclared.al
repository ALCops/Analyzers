page 50100 [|MyApiPage|]
{
    PageType = API;
    APIPublisher = 'MyPublisher';
    APIGroup = 'MyGroup';
    EntityName = 'MyEntityName';
    EntitySetName = 'MyEntitySetName';
    SourceTable = MyTable;
    DelayedInsert = true;

    layout
    {
        area(Content)
        {
            repeater(Group)
            {
                field(id; Rec.SystemId) { }
                field(MyField; Rec.MyField) { }
                field(lastModifiedDateTime; Rec.SystemModifiedAt) { }
            }
        }
    }
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }