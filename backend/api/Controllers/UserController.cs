using backend.api.Services;
using backend.api.Models;   
using Microsoft.AspNetCore.Mvc;
using backend.api.interfaces;

namespace backend.api.Controllers;

[Controller]
[Route("api/[controller]")]
public class UserController : Controller
{
    private readonly UserService _userService;
    private readonly IConfiguration _configuration;

    public UserController(UserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    [HttpPost]
    [Route("signup")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateUserInterface body)
    {
        var user = new Users{};
        if (body.FirstName == null || body.LastName == null || body.Email == null || body.Password == null)
        {
            return BadRequest(new { message = "All fields are required" });
        }

        user.Username = body.FirstName + body.LastName;
        user.Email = body.Email;
        user.Password = user.EncryptPasswordBase64(body.Password);

        //TODO Check if user already exists in the database
        await _userService.CreateUserAsync(user);
        //TODO Generate JWT token and return it to the client

        return Ok(new { result = user });
    }

}