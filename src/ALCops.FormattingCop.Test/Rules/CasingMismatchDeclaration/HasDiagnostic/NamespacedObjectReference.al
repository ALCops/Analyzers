namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50000 MyCodeunit
{
    var
        FullyQualified: Record [|MYPUBLISHER|].[|MYEXTENSION|].[|MYAPPDOMAIN|].[|MYTABLE|];
        Unqualified: Record [|MYTABLE|];

    procedure Foo(p: Record [|MYPUBLISHER|].[|MYEXTENSION|].[|MYAPPDOMAIN|].[|MYTABLE|])
    var
        LocalQualified: Codeunit [|MYPUBLISHER|].[|MYEXTENSION|].[|MYAPPDOMAIN|].[|MYHELPER|];
    begin
    end;
}

table 50000 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

codeunit 50001 MyHelper { }
