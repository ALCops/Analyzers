codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Integer
    var
        MyTable: Record MyTable;
        MyEnum: Enum MyEnum;
        Total: Integer;
    begin
        case MyEnum of
            MyEnum::" ":
                begin
                    MyTable.SetLoadFields(MyTable.MyField);
                    if MyTable.FindSet() then
                        repeat
                            Total += MyTable.MyField;
                        until MyTable.Next() = 0;
                end;
            MyEnum::MyValue:
                if [|MyTable.FindSet()|] then
                    repeat
                        Total += MyTable.MyField;
                    until MyTable.Next() = 0;
            else
                if [|MyTable.FindSet()|] then
                    repeat
                        Total += MyTable.MyField;
                    until MyTable.Next() = 0;
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

enum 50100 MyEnum
{
    value(0; " ") { }
    value(1; MyValue) { }
}
