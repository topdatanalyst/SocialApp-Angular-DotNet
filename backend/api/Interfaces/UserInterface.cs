namespace backend.api.interfaces;

public class CreateUserInterface
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }   
    public string? Email { get; set; } 
    public string? Password { get; set; } 
}

public class LoginInterface
{
    public string? Email { get; set; } 
    public string? Password { get; set; } 
}