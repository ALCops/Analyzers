codeunit 50100 MyCodeunit
{
    var
        MyList: List of [[|TEXT|]];
        MyDict: Dictionary of [[|INTEGER|], [|TEXT|]];
        MyNestedList: List of [Dictionary of [Integer, [|TEXT|]]];
        MyInterfaceList: List of [[|INTERFACE|] "My Interface"];
        MyCodeList: List of [[|CODE|][20]];
        MyEnumList: List of [[|ENUM|] "My Enum"];
}

interface "My Interface" { }
enum 50100 "My Enum" { value(0; "My Value") { } }
