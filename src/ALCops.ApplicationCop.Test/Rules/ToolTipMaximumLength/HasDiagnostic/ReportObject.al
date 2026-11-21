report 50100 MyReport
{
    ProcessingOnly = true;
    // Exceeds maximum length of 200 characters
    ToolTip = [|'Lorem ipsum dolor sit amet invidunt sanctus eirmod accusam elitr eirmod elit invidunt aliquyam. Nonumy et at nonummy labore duo odio rebum sed sed sed. Amet lorem dolor. Amet ipsum dolor sit aliquip at'|];

    requestpage
    {
        layout
        {
            area(Content)
            {
                field(MyField; MyField)
                {
                    ToolTip = 'Specifies the value.';
                }
            }
        }
    }

    var
        MyField: Text;
}
