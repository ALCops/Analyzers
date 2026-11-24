codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Integer
    var
        MyTable: Record MyTable;
        Condition: Boolean;
        Total: Integer;
    begin
        MyTable.SetLoadFields(MyTable.MyField);
        while Condition do begin
            if [|MyTable.FindSet()|] then
                repeat
                    Total += MyTable.MyField;
                until MyTable.Next() = 0;
            Condition := Total < 100;
        end;
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
