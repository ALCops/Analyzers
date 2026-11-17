codeunit 50102 MyCodeunit
{
    procedure ForLoop()
    var
        i: Integer;
    begin
        for i := 1 to 10 do begin
            if i = 5 then
                [|continue|];
        end;
    end;

    procedure ForeachLoop()
    var
        Numbers: List of [Integer];
        i: Integer;
    begin
        foreach i in Numbers do
            if i = 5 then
                [|continue|];
    end;
}
