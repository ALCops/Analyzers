codeunit 50100 MyCodeunit
{
    internal procedure [|Get|](TextEncoding: TextEncoding) Encoding: Enum BlobEncoding
    begin
        case TextEncoding of
            TextEncoding::MSDos:
                Encoding := Encoding::MSDos;
            TextEncoding::UTF16:
                Encoding := Encoding::UTF16;
            TextEncoding::Windows:
                Encoding := Encoding::Windows;
            TextEncoding::UTF8:
                Encoding := Encoding::UTF8;
        end;
    end;

    internal procedure [|Get|](Encoding: Enum BlobEncoding) TextEncoding: TextEncoding
    begin
        case Encoding of
            Encoding::MSDos:
                TextEncoding := TextEncoding::MSDos;
            Encoding::UTF16:
                TextEncoding := TextEncoding::UTF16;
            Encoding::Windows:
                TextEncoding := TextEncoding::Windows;
            Encoding::UTF8:
                TextEncoding := TextEncoding::UTF8;
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