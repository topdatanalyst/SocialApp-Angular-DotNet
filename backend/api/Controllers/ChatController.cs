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
                    return BadRequest(new {success = false, message = "Content, Sender and Recever are required"});
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

        [HttpGet]
        [Route("GetMsgsByNums")]
        public async Task<IActionResult> GetMessagesByNumsBetwenTwoUsers([FromQuery] string from, [FromQuery] string firstuid, [FromQuery] string seconduid){
           
            if(string.IsNullOrEmpty(from)|| string.IsNullOrEmpty(firstuid) || string.IsNullOrEmpty(seconduid)){
                return BadRequest(new {success = false, message = "problem with provided query parameters."});
            }
 
            // Call the service to get messages by number
            // if from = 0, it will return all messages between the two users
            // if from > 0, it will return the last 'from' number of messages between the two users
            List<Message> msgs = await _chatService.GetMessageByNum(int.Parse(from), firstuid, seconduid);
            return Ok(new { success = true, msgs });
        }

        [HttpGet]
        [Route("GetUserUnreadedMessage")]
        public async Task<IActionResult> GetUserUnReadedMessage([FromQuery] string userid){
            
            if(string.IsNullOrEmpty(userid)){
                return BadRequest(new {message = "problem with provided query parameters."});
            }

            List<UnReadedMessages> urm = await _chatService.GetUserUnreadedmsgs(userid);

            int totalUnreadedMessageCount = urm.Sum(msg => msg.NumOfUnreadedMessages);

            return Ok(new {messages = urm, total = totalUnreadedMessageCount});
        }

    }
}