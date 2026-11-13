namespace MyPublisher.MyExtension.MyAppDomain;

using Microsoft.Sales.Document;
using Microsoft.Utilities;
using System.Reflection;

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