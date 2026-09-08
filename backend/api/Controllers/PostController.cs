using System.Security.Claims;
using backend.api.Interfaces;
using backend.api.Models;
using backend.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.api.Controllers
{
    
    [Controller]
    [Route("api/[controller]")]
    public class PostController: ControllerBase
    {
         private readonly PostService _postService;
        private readonly IConfiguration _configuration;

        public PostController(PostService postService, IConfiguration configuration)
        {
            _postService = postService;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("createPost"), Authorize]
        public async Task<IActionResult> CreatePost([FromBody] CraeteOrUpdatePostInterface body){
            
            var post = new Post{};

            if(body.Title == null || body.Message == null || body.SelectedFile == null){
                return BadRequest(new {message = "proplem with provided body data."});
            }

            // Assign the values from the request body to the post object
            post.Title = body.Title;
            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            post.Creator = userIDToken;
            post.Message = body.Message;
            post.SelectedFile = body.SelectedFile;

            // Call the PostService to create a new post
            await _postService.CreateOnePostAsync(post);

            if(post == null){
                return BadRequest(new {message = "some thing went worng!."});
            }

            return Ok(new { post });
        }

        [HttpGet]
        [Route("getPostById/{id}")]
        public async Task<IActionResult> GetPostById([FromRoute] string id){
            if(id is null){
                return BadRequest(new {message = "proplem with provided id"});
            }
            var post = new Post{};
            post = await _postService.GetPostByID(id);

            if(post is null) return NotFound(new {message = "post not found", Success = false});

            return Ok(new {post = post});
        }

    }
}