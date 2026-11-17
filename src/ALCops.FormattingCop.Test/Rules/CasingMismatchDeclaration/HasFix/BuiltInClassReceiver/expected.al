codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        Xmlport.Run(Xmlport::"My Xmlport");
    end;
}

xmlport 50100 "My Xmlport" { schema { textelement(NodeName1) { } } }
