using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class MessagesController: BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;
        public MessagesController(IUserRepository userRepository, IMessageRepository messageRepository, IMapper mapper)
        {
            _mapper = mapper;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.GetUsername();

            if(username == createMessageDto.ReceiverUsername.ToLower())
            {
                return BadRequest("Cannot message yourself");
            }

            var sender = await _userRepository.GetUserbyUsernameAsync(username);
            var receiver =  await _userRepository.GetUserbyUsernameAsync(createMessageDto.ReceiverUsername);

            if(receiver == null) return NotFound();

            var message = new Message
            {
                Sender = sender,
                Receiver = receiver,
                SenderUsername = sender.UserName,
                ReceiverUsername = receiver.UserName,
                Content = createMessageDto.Content
            };

            _messageRepository.AddMessage(message);

            if(await _messageRepository.SaveAllAsync()) return Ok(_mapper.Map<MessageDto>(message));

            return BadRequest("Failed to send message");
        }

        [HttpGet]

        public async Task<ActionResult<PagedList<MessageDto>>> GetMessagesForUser([FromQuery]
        MessageParams messageParams)
        {
            messageParams.Username = User.GetUsername();

            var messages = await _messageRepository.GetMessageForUser(messageParams);

            Response.AddPaginationHeader(new PaginationHeader
            (messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages));

            return messages;

        }
        
        [HttpGet("thread/{username}")]

        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string username)
        {
            var currentUserName = User.GetUsername();

            return Ok(await _messageRepository.GetMessageThread(currentUserName, username));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUsername();
            
            var message = await _messageRepository.GetMessage(id);

            if(message.SenderUsername != username && message.ReceiverUsername != username)
            return Unauthorized();

            if(message.SenderUsername == username) message.SenderDeleted = true;
            if(message.ReceiverUsername == username) message.ReceiverDeleted = true;

            if(message.SenderDeleted && message.ReceiverDeleted)
            {
                _messageRepository.DeleteMessage(message);
            }

            if(await _messageRepository.SaveAllAsync()) return Ok();

            return BadRequest("Error deleting the message");

        }
    }


}