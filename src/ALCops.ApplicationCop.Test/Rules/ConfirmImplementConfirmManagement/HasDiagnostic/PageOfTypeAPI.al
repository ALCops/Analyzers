page 50100 MyPage
{
    PageType = API;
    APIPublisher = 'MyCompany';
    APIGroup = 'MyAPIGroup';
    EntityName = 'MyEntity';
    EntitySetName = 'MyEntities';
    DelayedInsert = true;

    procedure MyProcedure()
    begin
        if [|Confirm('Are You Sure?')|] then;
    end;
}