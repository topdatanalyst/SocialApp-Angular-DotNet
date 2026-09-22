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
        private readonly NotificationService _notificationService;
        

        public PostController(PostService postService,
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
            var post = new Post{};
            post = await _postService.GetPostByID(id);

            if(post is null) return NotFound(new {message = "post not found", Success = false});

            return Ok(new {post = post});
        }

        [HttpPost]
        [Route("{id}/commentPost"), Authorize]
        public async Task<IActionResult> CommentPost([FromRoute] string id, [FromBody] CommentBodyInterface body){

            if(body.Value is null || id is null){
                return BadRequest(new {message = "proplem with provided body data id or comment value"});
            }

            var post = await _postService.GetPostByID(id);
            if(post is null) return NotFound(new {message = "post not found", Success = false});

            // Add the comment to the post's comments collection
            post.Comments.Add(body.Value);

            // Update the post in the database
            var npost = await _postService.UpdatePost(id, post);

            if(npost is null) return NotFound(new {message = "proplem with prodived value", Success = false});

            // Check userID
            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if(post.Creator != null && userIDToken != null)
            {
                // Call notification Start 
                var user = new User{};
                user = await _postService.GetUsByid(userIDToken);
                if (user is not null){
                    //send notification to user2 that user1 is following them         
                    var deat = user.Username + " Comment On Your Post";
                    var usin = new UserIn{Name = user.Username, Avatar = user.ImageUrl};
                    var notification = new Notification {
                        Mainuid = post.Creator,
                        Targetid =id,
                        Details = deat,
                        user = usin
                    };
            
                    await _notificationService.CreateNotification(notification);      
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

            // Validate the provided id
            if(id == "undefind") return BadRequest(new {message = "proplem with provided id"});

            var user = new User{};
            // Retrieve the user by id using the PostService
            user = await _postService.GetUsByid(id);

            if(user is null || user.Id is null){
                return NotFound(new {message = "user with given id is not found."});
            }
 
            // Get the list of user IDs that the user is following
            var ides = user.Following;
            ides.Add(user.Id.ToString());

            return Ok(_postService.Query(ides, Page));
        }

        [HttpPatch]
        [Route("{id}/updatePost"), Authorize]
        public async Task<IActionResult> UpdatePost([FromRoute] string id, [FromBody] CraeteOrUpdatePostInterface body){

            if(body.Title == null || body.Message == null || body.SelectedFile == null){
                return BadRequest(new {message = "proplem with provided body data."});
            }

            // Get the user ID from the JWT token   
            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if (userIDToken is null){
                return NotFound(new {message = "Not Authorized."});
            }

            // Retrieve the post by ID using the PostService
            // id is the post ID
            var post = new Post{};
            post = await _postService.GetPostByID(id);

            if (post is null){
                return NotFound(new {message = "post with given id is not found.."});
            }

            if (userIDToken != post.Creator){
                return Unauthorized(new {message = "Not Authorized. you are not the creator of post"});

            }

            // Update the post properties with the values from the request body
            post.Title = body.Title?? post.Title;
            post.Message = body.Message?? post.Message;
            post.SelectedFile = body.SelectedFile?? post.SelectedFile;

            // Update the post using the PostService
            var upPost = await _postService.UpdatePost(id, post);
            if (upPost is null){
                return BadRequest(new {message = "can not update the post."});
            }

            return Ok(new { post = post });
        }

        [HttpPatch]
        [Route("{id}/likePost"), Authorize]
        public async Task<IActionResult> LikeDisLikePost([FromRoute] string id){
            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if (userIDToken is null){
                return NotFound(new {message = "Not Authorized."});
            }
            
            var post = new Post{};
            post = await _postService.GetPostByID(id);
            
            if (post is null){
                return NotFound(new {message = "post with given id is not found.."});
            }

            if(post.Likes.Contains(userIDToken)){
                post.Likes.Remove(userIDToken);
            } else {
                post.Likes.Add(userIDToken);
                //Call Notification .. notofy the user about the new user like about the post
                if (post.Creator != null){                     
                    var user = new User{};
                    user = await _postService.GetUsByid(userIDToken);
                    if (user is not null){    
                       //TODO send notification to user2 that user1 is following them                              
                        var deat = user.Username + " Like Your Post";
                        var usin = new UserIn{Name = user.Username, Avatar = user.ImageUrl};
                        // Created notification object
                        var notification = new Notification {
                            Mainuid = post.Creator,
                            Targetid =id,
                            Details = deat,
                            user = usin
                        };                    
                        await _notificationService.CreateNotification(notification);
                    }
                }
            }

            // upate post up
            var upPost = await _postService.UpdatePost(id, post);
            if (upPost is null){
                return BadRequest(new {message = "can not update the post."});
            }    

            return Ok(new {post = post});  

        }

        [HttpDelete]
        [Route("{id}/deletePost"), Authorize]
        public async Task<IActionResult>  DeletePost([FromRoute] string id){
            var userIDToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if (userIDToken is null){
                return NotFound(new {message = "Not Authorized."});
            }
            
            var post = new Post{};
            post = await _postService.GetPostByID(id);
            
            if (post is null){
                return NotFound(new {message = "post with given id is not found.."});
            }

            if (userIDToken != post.Creator){
                return Unauthorized(new {message = "Not Authorized. you are not the creator of post"});
            }

            await _postService.DeletePostAsync(id);
            return Ok(new {message = "post Deleted Successfully."});

        }



    }
}