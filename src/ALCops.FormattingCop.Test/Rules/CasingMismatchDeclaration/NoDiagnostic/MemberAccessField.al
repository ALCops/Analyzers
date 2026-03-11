table 50100 "My Table"
{
    fields
    {
        field(1; "My Field"; Integer) { }
        field(2; "Tax Code"; Code[20]) { }
    }

    procedure MyProcedure()
    begin
        Rec.[|"My Field"|] := 0;
        Rec.[|"Tax Code"|] := '';
    end;
}
