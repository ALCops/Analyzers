codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        MyEnum: Enum MyEnum;
    begin
        case MyEnum of
            MyEnum::" ":
                begin
                    MyTable.SetLoadFields(MyTable.MyField);
                    MyTable.Get();
                end;
            MyEnum::MyValue:
                [|MyTable.Get()|];
            else
                [|MyTable.Get()|];
        end;
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
