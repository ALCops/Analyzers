namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TempMyTable: Record MyPublisher.MyExtension.MyAppDomain.MyTable temporary;
        MyPage: Page MyPublisher.MyExtension.MyAppDomain.MyPage;
    begin
        [|MyPage.SetRecord(TempMyTable)|];
    end;
}

page 50100 MyPage { SourceTable = MyTable; }
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
