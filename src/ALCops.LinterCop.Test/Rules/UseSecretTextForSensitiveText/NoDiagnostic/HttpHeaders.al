codeunit 50100 MyCodeunit
{
    var
        RequestHeaders: HttpHeaders;

    procedure Add(MyText: SecretText)
    begin
        RequestHeaders.Add('Authorization', [|MyText|]);
    end;

    procedure TryAddWithoutValidation(MyText: SecretText)
    begin
        RequestHeaders.TryAddWithoutValidation('Authorization', [|MyText|]);
    end;

    procedure GetValues()
    var
        MyText: array[1] of SecretText;
    begin
        RequestHeaders.GetSecretValues('Authorization', [|MyText|]);
    end;
}