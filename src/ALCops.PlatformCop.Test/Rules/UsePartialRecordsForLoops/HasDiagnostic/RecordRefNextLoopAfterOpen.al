codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyRecordRef: RecordRef;
        Result: Text;
    begin
        MyRecordRef.Open(Database::MyTable);
        if [|MyRecordRef.Find('-')|] then
            repeat
                Result += Format(MyRecordRef.Field(2).Value());
            until MyRecordRef.Next() = 0;
        exit(Result);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}
