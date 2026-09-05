codeunit 50100 MyCodeunit
{
    procedure [|GetJsonToken|](JsonObject: JsonObject; TokenKey: Text) JsonToken: JsonToken
    begin
        if not JsonObject.Get(TokenKey, JsonToken) then
            Error('Token not found.');
    end;
}