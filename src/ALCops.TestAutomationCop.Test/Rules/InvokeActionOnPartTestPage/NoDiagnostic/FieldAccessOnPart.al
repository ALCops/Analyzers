table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

page 50100 MyListPart
{
    PageType = ListPart;
    SourceTable = MyTable;

    layout
    {
        area(content)
        {
            field(MyField; Rec.MyField) { }
        }
    }
}

codeunit 50100 MyTestCodeunit
{
    Subtype = Test;

    [Test]
    procedure MyTest()
    var
        SubPage: TestPage MyListPart;
        FieldValue: Integer;
    begin
        SubPage.OpenView();
        [|FieldValue := SubPage.MyField.AsInteger()|];
    end;
}
