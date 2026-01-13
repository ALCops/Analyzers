page 50100 MyApiPage
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
                field(MyField; Rec.MyField)
                {
                    [|ApplicationArea = All;|]
                }
            }
        }
    }

}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }