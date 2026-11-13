namespace MyPublisher.MyExtension.MyAppDomain;

using microsoft.utilities;

codeunit 50100 MyCodeunit
{
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