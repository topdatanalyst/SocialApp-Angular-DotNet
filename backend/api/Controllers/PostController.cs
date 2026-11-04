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
        private readonly IPostService _postService;
        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;
        

        public PostController(IPostService postService,
         IConfiguration configuration,
         NotificationService notificationService)
        {
            _postService = postService;
            _configuration = configuration;
            _notificationService = notificationService;
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
            // Call the PostService to retrieve the post by ID
            var post = await _postService.GetPostByID(id);

            if(post is null) return NotFound(new {message = "post not found", Success = false});

            return Ok(new { post });
        }

        [HttpPost]
        [Route("{id}/commentPost"), Authorize]
        public async Task<IActionResult> CommentPost([FromRoute] string id, [FromBody] CommentBodyInterface body){

             if(body.Value is null || id is null){
                return BadRequest(new {message = "proplem with provided body data id or comment value"});
             }
            var post = await _postService.GetPostByID(id);
            if(post is null) return NotFound(new {message = "post not found", Success = false});

            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if(userIDToken is null) return Unauthorized();

            // Call the PostService to create a comment for the post
            await _postService.CreateComment(id, userIDToken, body.Value);

            if(post.Creator != null && userIDToken != null ){
            // Call Notification .. notofy the user about the new user comment about the post
            var user = await _postService.GetUsByid(userIDToken);
                if (user is not null){                            
                    var deat = user.Username + " Comment On Your Post";
                    var us = new UserIn{Name = user.Username, Avatar = user.ImageUrl};
                    var nofification = new Notification {
                        Mainuid = post.Creator,
                        Targetid =id,
                        Details = deat,
                        User = us
                    };                
                    await _notificationService.CreateNotification(nofification);
                }
            }
            return Ok(new {data=post});

        }

        [HttpGet]
        [Route("searchPost")]
        public async Task<IActionResult> SearchForUsersPost([FromQuery] string searchQuery){

            if(searchQuery is null){
            return BadRequest(new {message = "proplem with provided serchquery"});
            }

            var posts = new List<Post>();
            var users = new List<User>();

            (posts, users) = await _postService.Search(searchQuery);

            return Ok(new {posts= posts, user = users});
        }

        [HttpGet]
        [Route("getpostsPagenation")]
        public async Task<IActionResult> GetPostsPagenationAsync([FromQuery] int Page, [FromQuery] string id){

            if(id == "undefind") return BadRequest(new {message = "proplem with provided id"});

            var user = await _postService.GetUsByid(id);

            if(user is null || user.Id is null){
                return NotFound(new {message = "user with given id is not found."});
            }

            var ides = user.Following;
            ides.Add(user.Id.ToString());

            return Ok(await _postService.Query(ides, Page));
        }

        [HttpPatch]
        [Route("{id}/updatePost"), Authorize]
        public async Task<IActionResult> UpdatePost([FromRoute] string id, [FromBody] CraeteOrUpdatePostInterface body){

            if(body.Title == null || body.Message == null || body.SelectedFile == null){
                return BadRequest(new {message = "proplem with provided body data."});
            }

            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if (userIDToken is null){
                return NotFound(new {message = "Not Authorized."});
            }

            var postResponse = await _postService.GetPostByID(id);

            if (postResponse is null){
                return NotFound(new {message = "post with given id is not found.."});
            }

            if (userIDToken != postResponse.Creator){
                return Unauthorized(new {message = "Not Authorized. you are not the creator of post"});

            }

            if (body.Title is null && body.Message is null && body.SelectedFile is null)
            {
                return BadRequest(new {message = "proplem with profived body data."});
            }

            // Create a new Post object with updated values, keeping the existing values if not provided in the request body
            var postUpdate = new Post
            {
                Id = id,
                Creator = postResponse.Creator ,
                Title = body.Title ?? postResponse.Title,
                Message = body.Message ?? postResponse.Message,
                SelectedFile = body.SelectedFile ?? postResponse.SelectedFile,
                Likes = postResponse.Likes,
                CreatedAt = postResponse.CreatedAt ?? DateTime.Now,
            };    

            // Call the PostService to update the post
            var upPost = await _postService.UpdatePost(id, postUpdate);
            if (upPost is null){
                return BadRequest(new {message = "can not update the post."});
            }    

            return Ok(new {post = upPost});    
        }

        [HttpPatch]
        [Route("{id}/likePost"), Authorize]
        public async Task<IActionResult> LikeDisLikePost([FromRoute] string id){
            // Get the user ID from the authenticated user's claims
            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if (userIDToken is null){
                return NotFound(new {message = "Not Authorized."});
            }
            
            var postResponse = await _postService.GetPostByID(id);
            
            if (postResponse is null){
                return NotFound(new {message = "post with given id is not found.."});
            }

            var likes = postResponse.Likes ?? new List<string>();

            if(likes.Contains(userIDToken)){
                likes.Remove(userIDToken);
            } else {
            likes.Add(userIDToken);
                // TODO Call Notification Service to notify the post creator about the like
                if (postResponse.Creator != null){
                        var user = new User{};
                    user = await _postService.GetUsByid(userIDToken);
                    if (user is not null){
                                
                        var deat = user.Username + " Like Your Post";
                        var us = new UserIn{Name = user.Username, Avatar = user.ImageUrl};
                        var nofification = new Notification {
                            Mainuid = postResponse.Creator,
                            Targetid =id,
                            Details = deat,
                            User = us
                        };
                        
                        await _notificationService.CreateNotification(nofification);
                    }
                }
            }

            // Create a new Post object with updated likes
            var postUpdate = new Post
            {
                Id = id,
                Creator = postResponse.Creator ,
                Title =  postResponse.Title,
                Message =  postResponse.Message,
                SelectedFile =  postResponse.SelectedFile,
                Likes = likes,
                CreatedAt = postResponse.CreatedAt ?? DateTime.Now,
            };    

            // Update the post with the new likes   
            var upPost = await _postService.UpdatePost(id, postUpdate);
            if (upPost is null){
                return BadRequest(new {message = "can not update the post."});
            }    

            var enrichedPost = await _postService.GetPostByID(id);
            return Ok(new {post = enrichedPost});  

        }

        [HttpDelete]
        [Route("{id}/deletePost"), Authorize]
        public async Task<IActionResult>  DeletePost([FromRoute] string id){
            // Get the user ID from the authenticated user's claims
           var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if (userIDToken is null){
                return NotFound(new {message = "Not Authorized."});
            }
            
            var post = await _postService.GetPostByID(id);
            
            if (post is null){
                return NotFound(new {message = "post with given id is not found.."});
            }

            if (userIDToken != post.Creator){
                return Unauthorized(new {message = "Not Authorized. you are not the creator of post"});
            }

            await _postService.DeletePostAsync(id);
            return Ok(new {message = "post Deleted Successfully."});
        }

        [HttpDelete]
        [Route("/comments/{postId}/comments/{commentId}"), Authorize] 
        public async Task<IActionResult> DeleteComment([FromRoute] string postId, [FromRoute] string commentId)
        {
            // Get the user ID from the authenticated user's claims
            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if (userIDToken is null){
                    return NotFound(new {message = "Not Authorized."});
            }
        
            // Check if the post exists
            var post = await _postService.GetPostByID(postId);
            if (post is null) return NotFound(new {message = "post not found"});

            var postCreatorId = post.Creator ?? "unnown";
            
            // Call the PostService to delete the comment
            try
            {
            var deleted = await _postService.DeleteComment(commentId, userIDToken, postCreatorId);
            if (!deleted) return NotFound(new {message = "comment not found"});

            return Ok(
                new
                {
                    message = "Comment Deleted Successfully",
                    deletedCommentId = commentId
                });

            } catch (UnauthorizedAccessException)
           {
                return Unauthorized(new {message = "You are not authoreized to delete this comment"});
            }
        }

    }
}