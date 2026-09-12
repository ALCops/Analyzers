namespace MyPublisher.MyExtension.Setup;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.FindFirst()|];
        exit(MyTable.MyField);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Source Code"; Code[10]) { }
        field(2; "Entry No."; Integer)
        {
            AutoIncrement = true;
        }
        field(3; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Source Code", "Entry No.") { }
    }
}
