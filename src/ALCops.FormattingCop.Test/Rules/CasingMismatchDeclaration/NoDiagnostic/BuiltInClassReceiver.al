codeunit 50105 MyCodeunit
{
    procedure MyProcedure()
    begin
        [|Codeunit|].Run(Codeunit::MyCodeunit);
        [|Codeunit|].Run(Codeunit::MyCodeunit);
        [|Xmlport|].Run(Xmlport::"My Xmlport");
        [|Xmlport|].Run(Xmlport::"My Xmlport");
    end;
}

xmlport 50105 "My Xmlport" { schema { textelement(NodeName1) { } } }
