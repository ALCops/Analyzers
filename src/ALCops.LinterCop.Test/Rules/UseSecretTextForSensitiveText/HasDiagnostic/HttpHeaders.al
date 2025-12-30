codeunit 50100 MyCodeunit
{
    var
        RequestHeaders: HttpHeaders;

    procedure Add(MyText: Text)
    begin
        RequestHeaders.Add('Authorization', [|MyText|]);
    end;

    procedure TryAddWithoutValidation(MyText: Text)
    begin
        RequestHeaders.TryAddWithoutValidation('Authorization', [|MyText|]);
    end;

    procedure GetValues()
    var
        MyText: array[1] of Text;
    begin
        RequestHeaders.GetValues('Authorization', [|MyText|]);
    end;
}