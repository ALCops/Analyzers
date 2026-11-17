codeunit 50100 MyCodeunit
{
    procedure Lookup(var SelectedId: Integer): Boolean
    var
        TempMyTable: Record MyTable temporary;
        MyLookupPage: Page MyLookupPage;
    begin
        TempMyTable.MyField := SelectedId;
        MyLookupPage.LoadRecords();
        [|MyLookupPage.SetRecord(TempMyTable)|];
        MyLookupPage.SetTableView(TempMyTable);
        MyLookupPage.LookupMode(true);
        if MyLookupPage.RunModal() = Action::LookupOK then begin
            MyLookupPage.GetRecord(TempMyTable);
            SelectedId := TempMyTable.MyField;
            exit(true);
        end;
        exit(false);
    end;
}

page 50100 MyLookupPage
{
    PageType = List;
    SourceTable = MyTable;
    SourceTableTemporary = true;

    layout
    {
        area(Content)
        {
            repeater(Lines)
            {
                field(MyField; Rec.MyField) { }
            }
        }
    }

    procedure LoadRecords()
    begin
        Rec.Reset();
        Rec.DeleteAll();
        Rec.MyField := 1;
        Rec.Insert();
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
