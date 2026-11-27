codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyHelper: Codeunit MyHelper;
        MyText: Text;
    begin
        MyHelper.SetText('x-ms-file-attributes', [|'None'|]);
        MyHelper.AreEqual([|'Strong'|], MyText, 'Unexpected value');
        MyText := StrSubstNo('%1', [|'Standard'|]);
        SetStyle([|'Unfavorable'|]);
    end;

    local procedure SetStyle(NewStyle: Text)
    begin
    end;
}

codeunit 50101 MyHelper
{
    procedure SetText(Name: Text; Value: Text)
    begin
    end;

    procedure AreEqual(Expected: Variant; Actual: Variant; Msg: Text)
    begin
    end;
}
