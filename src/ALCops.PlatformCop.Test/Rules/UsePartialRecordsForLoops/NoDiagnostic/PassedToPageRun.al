codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        if [|MyTable.FindSet()|] then
            repeat
                PAGE.Run(PAGE::MyPage, MyTable);
            until MyTable.Next() = 0;
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

page 50100 MyPage
{
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field("No."; Rec."No.") { }
            field(MyField; Rec.MyField) { }
        }
    }
}
