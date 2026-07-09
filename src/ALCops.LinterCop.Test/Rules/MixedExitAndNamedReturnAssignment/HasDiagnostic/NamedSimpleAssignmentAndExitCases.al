codeunit 50101 NmdSimpleAssignExit
{
    procedure NamedAssignmentAndExitValue() Result: Integer
    begin
        Result := 1;

        [|exit(2);|]
    end;

    procedure NamedAssignmentAndExitWithoutValue() Result: Integer
    begin
        Result := 1;

        [|exit;|]
    end;
}
