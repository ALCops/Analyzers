namespace MyPublisher.MyExtension.MyAppDomain;

using Microsoft.Utilities;

codeunit 50100 MyCodeunit
{
    var
        PageManagement: Codeunit "Page Management";

    procedure MyProcedure()
    var
        SalesHeader: Record "Sales Header";
    begin
        [|Page.Run(0, SalesHeader)|];
    end;
}

table 36 "Sales Header"
{
    fields { field(1; MyField; Integer) { } }
}
/*EOD*/
namespace Microsoft.Utilities;

codeunit 700 "Page Management" { }