codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyParam: [|XmlPort|] "My Xmlport")
    var
        MyPort: [|XmlPort|] "My Xmlport";
    begin
    end;
}

xmlport 50100 "My Xmlport" { schema { textelement(NodeName1) { } } }
