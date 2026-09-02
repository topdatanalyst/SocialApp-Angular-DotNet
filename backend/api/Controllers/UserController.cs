using backend.api.Services;
using backend.api.Models;   
using Microsoft.AspNetCore.Mvc;
using backend.api.interfaces;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

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

        // Check if user already exists in the database 
        var checkUser = await _userService.GetUserByEmailAsync(user.Email);
        if (checkUser != null)
        {
            return Conflict(new { message = "User already exists" });
        }  

        await _userService.CreateUserAsync(user);
        //TODO Generate JWT token and return it to the client
        // Create claims for the JWT token
            var claims = new List<Claim>    
            {
                new Claim(ClaimTypes.Name, user.Username ?? throw new InvalidOperationException("Username is null.") ),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString() ?? throw new InvalidOperationException("User ID is null.")),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString() ?? throw new InvalidOperationException("User ID is null.")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            // Get the JWT secret key from configuration
            var tokenSecret = _configuration.GetValue<string>("JwtSecret:SecretKey");
            // Create a symmetric security key using the secret key
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(tokenSecret ?? throw new InvalidOperationException("JWT secret key is not configured.")));    
            // Create signing credentials using the security key and HMAC SHA256 algorithm
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); 
            var expires = DateTime.UtcNow.AddHours(2);
            // Create the JWT token with issuer, audience, claims, expiration, and signing credentials  
            var token = new JwtSecurityToken(
                issuer: "https://localhost:7206",
                audience: "https://localhost:7206",
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

        return Ok(new { result = user, token = new JwtSecurityTokenHandler().WriteToken(token), expiration = expires });
    }

    [HttpPost]
    [Route("signin")]
    public async Task<IActionResult> LogInUser([FromBody] LoginInterface body)
    {
        if(body.Email == null || body.Password == null)
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        // Check if user exists in the database
        var user = await _userService.GetUserByEmailAsync(body.Email);
        var decodedPassword = user?.DecryptPasswordBase64(user.Password);   
        if (user is  null)
        {
            return NotFound(new { message = "User not found" });
        } else if (decodedPassword != body.Password)
        {
            return Unauthorized(new { message = "Invalid password" });
        }else
        {
            //TODO Generate JWT token and return it to the client
            // Create claims for the JWT token
            var claims = new List<Claim>    
            {
                new Claim(ClaimTypes.Name, user.Username ?? throw new InvalidOperationException("Username is null.") ),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString() ?? throw new InvalidOperationException("User ID is null.")),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString() ?? throw new InvalidOperationException("User ID is null.")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            // Get the JWT secret key from configuration
            var tokenSecret = _configuration.GetValue<string>("JwtSecret:SecretKey");
            // Create a symmetric security key using the secret key
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(tokenSecret ?? throw new InvalidOperationException("JWT secret key is not configured.")));    
            // Create signing credentials using the security key and HMAC SHA256 algorithm
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); 
            var expires = DateTime.UtcNow.AddHours(2);
            // Create the JWT token with issuer, audience, claims, expiration, and signing credentials  
            var token = new JwtSecurityToken(
                issuer: "https://localhost:7206",
                audience: "https://localhost:7206",
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return Ok(new { result = user, token = new JwtSecurityTokenHandler().WriteToken(token), expiration = expires });
        }   
    }

    [HttpGet]
    [Route("getUser/{id}")]    
    public async Task<IActionResult> GetUserById([FromRoute] string id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user is null)
            {
                return NotFound(new { message = "User not found" });
            }

            // TODO return also the user posts 

            return Ok(new { result = user, posts = Array.Empty<object>() }); 

        }         
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the user", error = ex.Message });
        }
    }

    [HttpPut]
    [Route("updateUser/{id}"), Authorize]
    public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] UpdateUserInterface body)
    {
        try
        {
            if (body.UserName == null || body.ImageUrl == null || body.Bio == null)
            {
                return BadRequest(new { message = "At least one field is required to update" });
            }

            // Check if the user is authorized to update the user   
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId?.ToString() != id)
            {
                return Unauthorized(new { message = "You are not authorized to update this user" });
            }

            // Check if the user exists in the database
            var user = await _userService.GetUserByIdAsync(id);
            if (user is null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.Username = body.UserName;
            user.ImageUrl = body.ImageUrl;  
            user.Bio = body.Bio;    

            var updatedUser = await _userService.UpdateUserAsync(id, user);
            if (updatedUser is null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok( new { message = "User updated successfully", result = updatedUser });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the user", error = ex.Message });
        }
    }

    [HttpPatch]
    [Route("test"), Authorize]
    public IActionResult Test()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(new { message = "Authorized", userId });
    }
}