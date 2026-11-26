page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            fileuploadaction(MyFileUploadAction)
            {
                [|AboutText = 'My file upload action explains itself.', Locked = true|];

                trigger OnAction(Files: List of [FileUpload])
                begin
                end;
            }
        }
    }
}
