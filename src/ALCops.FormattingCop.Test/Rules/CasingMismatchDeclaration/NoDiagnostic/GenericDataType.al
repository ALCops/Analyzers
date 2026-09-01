codeunit 50100 MyCodeunit
{
    var
        MyList: List of [[|Text|]];
        MyDict: Dictionary of [[|Integer|], [|Text|]];
        MyNestedList: List of [Dictionary of [Integer, [|Text|]]];
        MyInterfaceList: List of [[|Interface|] "My Interface"];
        MyCodeList: List of [[|Code|][20]];
        MyEnumList: List of [[|Enum|] "My Enum"];
}

interface "My Interface" { }
enum 50100 "My Enum" { value(0; "My Value") { } }
