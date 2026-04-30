codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        SimpleMsg: Label 'Hello %1', Comment = '%1=Name';
        CompleteText: Text;
    begin
        // AA0131 handles this case (too many args with at least 1 arg passed)
        CompleteText := StrSubstNo([|SimpleMsg|], 'World', 'Extra');
    end;
}
