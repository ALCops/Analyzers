namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50000 MyCodeunit
{
    var
        FullyQualified: Record [|MyPublisher|].[|MyExtension|].[|MyAppDomain|].[|MyTable|];
        Unqualified: Record [|MyTable|];

    procedure Foo(p: Record [|MyPublisher|].[|MyExtension|].[|MyAppDomain|].[|MyTable|])
    var
        LocalQualified: Codeunit [|MyPublisher|].[|MyExtension|].[|MyAppDomain|].[|MyHelper|];
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
