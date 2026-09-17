using backend.api.Interfaces;
using backend.api.Models;
using backend.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : Controller
    {
         private readonly IConfiguration _configuration;
         private readonly ChatService _chatService; 

        public ChatController(IConfiguration configuration, ChatService chatService)
        {
            _configuration = configuration;
            _chatService = chatService;
        }

        [HttpPost]
        [Route("SendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageInterface body){

            if(body.Content ==null|| body.Sender ==null|| body.Recever ==null){
                            return BadRequest();
            }

            var msg = new Message
            {
                Content = body.Content,
                Sender = body.Sender,
                Recever = body.Recever
            };
    
            await _chatService.SendMessageAsync(msg, body.Sender, body.Recever);
            if(msg == null){
                return BadRequest();
            }
            return Ok(new {success = true});

        }
    }
}