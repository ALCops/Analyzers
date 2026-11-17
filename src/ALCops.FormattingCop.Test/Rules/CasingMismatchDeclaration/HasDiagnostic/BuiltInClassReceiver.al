codeunit 50105 MyCodeunit
{
    procedure MyProcedure()
    begin
        [|CODEUNIT|].Run(Codeunit::MyCodeunit);
        [|codeunit|].Run(Codeunit::MyCodeunit);
        [|XMLPORT|].Run(Xmlport::"My Xmlport");
        [|XmlPort|].Run(Xmlport::"My Xmlport");
    end;
}

xmlport 50105 "My Xmlport" { schema { textelement(NodeName1) { } } }
