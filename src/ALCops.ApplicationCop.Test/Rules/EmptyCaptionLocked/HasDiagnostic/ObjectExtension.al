table 50100 MyTable
{
    Caption = 'My Table';

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = 'My Field';
        }
    }
}

page 50100 MyPage
{
    SourceTable = MyTable;
    Caption = 'My Page';
}

tableextension 50100 MyTableExt extends MyTable
{
    [|Caption|] = '';
}

pageextension 50100 MyPageExt extends MyPage
{
    [|Caption|] = '';
}