codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyJsonToken: JsonToken;
        Results: List of [JsonToken];
    begin
        MyJsonToken.SelectTokens([|'$.custom_attributes[?(@.attribute_code == "activation_status")].value'|], Results);
    end;
}
