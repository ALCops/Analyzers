codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Integer
    var
        MyTable: Record MyTable;
        Condition: Boolean;
        Total: Integer;
    begin
        if Condition then begin
            MyTable.SetLoadFields(MyTable.MyField);
            if MyTable.FindSet() then
                repeat
                    Total += MyTable.MyField;
                until MyTable.Next() = 0;
        end else
            if [|MyTable.FindSet()|] then
                repeat
                    Total += MyTable.MyField;
                until MyTable.Next() = 0;
        exit(Total);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    keys
    {
        key(PK; MyField) { }
    }
}
