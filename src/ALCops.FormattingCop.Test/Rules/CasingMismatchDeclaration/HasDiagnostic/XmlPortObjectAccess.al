page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(Import)
            {
                trigger OnAction()
                begin
                    // The receiver of Run() is intentionally not analyzed (keyword-named identifier filter)
                    XmlPort.Run([|XmlPort|]::"My Xmlport");
                end;
            }
        }
    }
}

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        Xmlport.Run([|XMLPORT|]::"My Xmlport");
        Xmlport.Run([|xmlport|]::"My Xmlport", true);
    end;
}

xmlport 50100 "My Xmlport" { schema { textelement(NodeName1) { } } }
