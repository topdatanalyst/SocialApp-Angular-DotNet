namespace backend.api.Interfaces;   

public class CraeteOrUpdatePostInterface {
    public string? Title {get; set;}
    public string? Message {get; set;}
    public string? Creator {get; set;}
    public string? SelectedFile {get; set;}
}


public class CommentBodyInterface {
       public string? Value {get; set;}
 
}