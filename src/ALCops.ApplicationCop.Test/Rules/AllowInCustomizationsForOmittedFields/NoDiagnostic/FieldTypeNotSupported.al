table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Code[10]) { }
        field(2; [|MyBlobField|]; Blob) { }
        field(3; [|MyMediaField|]; Media) { }
        field(4; [|MyMediaSetField|]; MediaSet) { }
        field(5; [|MyRecordIdField|]; RecordId) { }
        field(6; [|MyTableFilterField|]; TableFilter) { }
    }
}

page 50000 MyPage { SourceTable = MyTable; }