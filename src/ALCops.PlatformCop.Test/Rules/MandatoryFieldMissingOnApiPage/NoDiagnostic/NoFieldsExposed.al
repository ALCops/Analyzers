page 50100 [|MyApiPage|]
{
    PageType = API;
    APIPublisher = 'myPublisher';
    APIGroup = 'myGroup';
    EntityName = 'myEntityName';
    EntitySetName = 'myEntitySetName';
    SourceTable = MyTable;
    DelayedInsert = true;
 
    [ServiceEnabled]
    procedure GetData(request: Text): Text
    begin
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
