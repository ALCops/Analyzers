codeunit 50100 CaseEveryBranchInitializes
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue(Selection: Integer)
    begin
        case Selection of
            1:
                MyValue := 10;
            2:
                MyValue := 20;
            else
                MyValue := 30;
        end;

        Message('%1', MyValue);
    end;
}
