namespace MyPublisher.MyExtension.MyAppDomain;

using Microsoft.Utilities;
#if not CLEAN25
using Microsoft.Sales.Document;
#endif

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        SalesHeader: Record "Sales Header";
        PageManagement: Codeunit "Page Management";
    begin
        PageManagement.PageRun(SalesHeader);
    end;
}

table 36 "Sales Header"
{
    fields { field(1; MyField; Integer) { } }
}
/*EOD*/
namespace Microsoft.Utilities;

codeunit 700 "Page Management" { }