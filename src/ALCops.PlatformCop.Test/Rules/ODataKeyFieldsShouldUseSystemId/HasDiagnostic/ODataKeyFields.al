page 50100 MyApiPage
{
    PageType = API;
    APIPublisher = 'MyPublisher';
    APIGroup = 'MyGroup';
    EntityName = 'MyEntityName';
    EntitySetName = 'MyEntitySetName';
    SourceTable = MyTable;
    DelayedInsert = true;
    ODataKeyFields = [|MyField|];

    layout
    {
        area(Content)
        {
            repeater(Group)
            {
                field(MyField; Rec.MyField) { }
            }
        }
    }
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }