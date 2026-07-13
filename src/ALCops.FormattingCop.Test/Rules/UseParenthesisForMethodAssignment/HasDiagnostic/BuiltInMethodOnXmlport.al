xmlport 50100 MyXmlport
{
    trigger OnInitXmlPort()
    begin
        [|currXMLport.TextEncoding := TextEncoding::Windows;|]
    end;
}
