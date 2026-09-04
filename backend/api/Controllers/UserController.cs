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

    [HttpPatch]
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
    [Route("{id}/following"), Authorize]
    public async Task<IActionResult> Following([FromRoute] string id)
    {
        if (id == null)
        {
            return BadRequest(new { message = "User ID is required" });
        }
        try
        {
            // Check if the user is authorized to follow the user2
            var user2 = await _userService.GetUserByIdAsync(id);
            if (user2 is null || user2.Id is null ) return NotFound (new {Message = "user Not found", Success = false});

            // Get the user ID from the JWT token
            // user is the user who is following user2
            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIDToken == null){
                return BadRequest(new {message = "proplem with provided id data of token."});
            }

            // user1 is the user who is following user2 
            var user1 = await _userService.GetUserByIdAsync(userIDToken.ToString());
            if (user1 is null || user1.Id is null ) return NotFound (new {Message = "user Not found", Success = false});
            
            // create a list of following and followers if they are null
            if(user1.Following == null){
                user1.Following = new List<string>{};
            }
            
            // check if user2 is already 
            if(user2.Followers == null){
                user2.Followers = new List<string>{};
            }

            // helper variable
            var fo = user1.Following;
            var fo2 = user2.Followers;

            // if user1 is already following user2, then unfollow, else follow
            if (fo.Contains(id)){
                fo.Remove(id);
                user1.Following = fo;
                fo2.Remove(user1.Id);
                user2.Followers = fo2;
            } else {
                fo.Add(id);
                user1.Following = fo;
                fo2.Add(user1.Id);
                user2.Followers = fo2;
                //TODO send notification to user2 that user1 is following them  
            }

            // update the users in the database
            await _userService.UpdateUserAsync(user1.Id.ToString(), user1);
            await _userService.UpdateUserAsync(user2.Id.ToString(), user2);


            return Ok(new {
                user1 = user1,
                user2 = user2, 
                Succes = true,
                Message = "Successfully."
            });
            
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "An error occurred while following the user", error = ex.Message });
        }

    }

    [HttpGet]
    [Route("getSuggested"), Authorize]
    public async Task<IActionResult> GetSuggestedUsers([FromQuery] string id)
    {
        try
        {
            if(id == "undefined") return BadRequest(new {Message = "id is undefined ", Success = false});

            var mainUser = await _userService.GetUserByIdAsync(id);
            if (mainUser is null) return  NotFound(new {Message = "user not found! ", Success = false});
        
            // get the following list of the user
            var FollowingList = mainUser.Following;
            if (FollowingList is null) return  NotFound(new {Message = "null follwing list for  user ", Success = false});

            // get the users that the user is following
            var FoloUsersList = new List<Users>{};
            foreach( var Uid in FollowingList)
            {
                var getuserFollwoing = await _userService.GetUserByIdAsync(Uid);
                if (getuserFollwoing != null){
                     FoloUsersList.Add(getuserFollwoing);
                }
            }
        
        // get the followers and following of the users that the user is following

        // is list to store the ids of the users that are already suggested to avoid duplicates
        var usersidesfrosug = new List<string>{};
        // final list of users to be suggested
        var FinalUsers = new List<Users>{};
        // loop through the users that the user is following and get their followers and following
        foreach (var us in FoloUsersList ){
            // followers
            if (us.Followers != null && mainUser.Id != null){
                foreach( var ids in us.Followers){
                    if (usersidesfrosug.Contains(ids) | ids == mainUser.Id.ToString()) continue;
                    var gus = await _userService.GetUserByIdAsync(ids);
                    if (gus != null) FinalUsers.Add(gus);
                    usersidesfrosug.Add(ids);
                }
            }
            // following
            if (us.Following != null && mainUser.Id != null){
                foreach( var ids in us.Following){
                    if (usersidesfrosug.Contains(ids) | ids == mainUser.Id.ToString()) continue;
                    var gus = await _userService.GetUserByIdAsync(ids);
                    if (gus != null) FinalUsers.Add(gus);
                    usersidesfrosug.Add(ids);
                }
            }   
        }

         // return the result 
            return Ok (new {
                Users = FinalUsers,
                Success = true,
                 Message = "Successfully"
            });
        }
        catch ( Exception ex)
        {
            
            return BadRequest(new {Message = ex.Message, Success = false});
        }
    }
    
    [HttpDelete]
    [Route("deleteUser/{id}"), Authorize]
    public async Task<IActionResult> DeleteUser([FromRoute] string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
        if (userId?.ToString() != id)
        {
            return Unauthorized(new { message = "You are not authorized to delete this user" });
        }
        await _userService.DeleteUserAsync(id);
        return Ok(new { message = "User deleted successfully" });
    }

    [HttpPatch]
    [Route("test"), Authorize]
    public IActionResult Test()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(new { message = "Authorized", userId });
    }
}