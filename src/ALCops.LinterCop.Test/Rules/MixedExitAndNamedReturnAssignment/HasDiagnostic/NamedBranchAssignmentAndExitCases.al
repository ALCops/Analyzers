codeunit 50100 NmdBranchAssignExit
{
    procedure NamedAssignmentInIfAndExitInElse(UseExit: Boolean) Result: Integer
    begin
        if UseExit then
            [|exit(2)|]
        else
            Result := 1;
    end;

    procedure NamedCaseAssignmentAndExit(Input: Integer) Result: Integer
    begin
        case Input of
            1:
                Result := 10;

            2:
                [|exit(20);|]

            else
                Result := 30;
        end;
    end;

    procedure NamedIfElseIfElseAssignmentAndExit(Input: Integer) Result: Integer
    begin
        if Input = 1 then
            Result := 10
        else if Input = 2 then
            [|exit(20)|]
        else
            Result := 30;
    end;

    procedure NamedNestedIfElseIfAssignmentAndExit(Outer: Boolean; Inner: Integer) Result: Integer
    begin
        if Outer then begin
            if Inner = 1 then
                Result := 10
            else if Inner = 2 then
                [|exit(20)|]
            else
                Result := 40;
        end else
            Result := 30;
    end;
}
