// API pages are excluded from this check
page 50100 MyApiPage
{
    PageType = API;
    SourceTable = MyTable;
    DelayedInsert = true;
    APIGroup = 'myGroup';
    APIPublisher = 'myPublisher';
    APIVersion = 'v1.0';
    EntityName = 'myEntity';
    EntitySetName = 'myEntities';

    layout
    {
        area(Content)
        {
            repeater(Lines)
            {
                [|field(myField; Rec.MyField) { }|]
                [|field(myField2; Rec.MyField2) { }|]
            }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; MyField; Integer) { }
        field(3; MyField2; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
