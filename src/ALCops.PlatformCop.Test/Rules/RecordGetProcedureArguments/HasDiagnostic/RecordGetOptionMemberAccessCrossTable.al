codeunit 50100 MyCodeunit
{
    procedure MyProcedure(ObjectID: Integer)
    var
        Table1: Record Table1;
        Table2: Record Table2;
    begin
        [|Table1.Get(Table2."Object Type"::Codeunit, ObjectID)|];
    end;
}

table 50100 Table1
{
    fields
    {
        field(1; "Object Type"; Option)
        {
            OptionMembers = "A","B","C";
        }
        field(3; "Object ID"; Integer)
        {
        }
    }
    keys
    {
        key(pk; "Object Type", "Object ID")
        {
        }
    }
}

table 50101 Table2
{
    fields
    {
        field(1; "Object Type"; Option)
        {
            OptionMembers = "TableData","Table",,"Report",,"Codeunit";
        }
    }
    keys
    {
        key(pk; "Object Type")
        {
        }
    }
}
