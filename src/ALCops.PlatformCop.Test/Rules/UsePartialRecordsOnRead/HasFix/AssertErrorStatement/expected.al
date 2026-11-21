codeunit 50100 MyCodeunit
{
    Subtype = Test;

    [Test]
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetLoadFields(MyField);
        asserterror MyTable.Get('X');
        Message(MyTable.MyField);
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