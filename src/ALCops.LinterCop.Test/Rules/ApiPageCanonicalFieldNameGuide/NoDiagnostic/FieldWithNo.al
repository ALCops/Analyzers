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
                field([|number|]; Rec."No.") { }
            }
        }
    }
}

table 50100 MyTable { fields { field(1; "No."; Code[20]) { } } }