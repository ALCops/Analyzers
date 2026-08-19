codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyParam: [|Xmlport|] "My Xmlport")
    var
        MyPort: [|XMLPORT|] "My Xmlport";
        MyOtherPort: [|xmlport|] "My Xmlport";
    begin
    end;
}

xmlport 50100 "My Xmlport" { schema { textelement(NodeName1) { } } }
