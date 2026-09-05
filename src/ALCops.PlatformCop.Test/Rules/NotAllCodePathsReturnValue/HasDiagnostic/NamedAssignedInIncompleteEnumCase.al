codeunit 50100 MyCodeunit
{
    procedure [|Get|](Encoding: Enum BlobEncoding) TextEncoding: TextEncoding
    begin
        case Encoding of
            Encoding::MSDos:
                TextEncoding := TextEncoding::MSDos;
            Encoding::UTF16:
                TextEncoding := TextEncoding::UTF16;
            Encoding::Windows:
                TextEncoding := TextEncoding::Windows;
        end;
    end;
}

enum 50100 BlobEncoding
{
    value(0; MSDos)
    {
    }
    value(1; UTF16)
    {
    }
    value(2; Windows)
    {
    }
    value(3; UTF8)
    {
    }
}