codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Text: Text): Text
    var
        [|_Text|]: Text;
        [|_MyVariable|]: Integer;
    begin
        _Text := Text;
        exit(_Text);
    end;
}
