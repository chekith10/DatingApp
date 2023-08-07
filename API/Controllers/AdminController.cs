using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
    
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _uow;
        private readonly IPhotoService _photoService;

        public AdminController(UserManager<AppUser> userManager, IUnitOfWork uow, IMapper mapper, 
        IPhotoService photoService)
        {
            _photoService = photoService;
            _uow = uow;
            _mapper = mapper;
            _userManager = userManager;

        }
        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            var users = await _userManager.Users.OrderBy(u => u.UserName)
            .Select(u => new
            {
                u.Id,
                Username = u.UserName,
                Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
            })
            .ToListAsync();
            return Ok(users);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("edit-roles/{username}")]

        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
        {
            if (string.IsNullOrEmpty(roles)) return BadRequest("User must have atleast one role");

            var selectedRoles = roles.Split(",").ToArray();

            var user = await _userManager.FindByNameAsync(username);

            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded) return BadRequest("Failed to add to roles");

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded) return BadRequest("Failed to remove from roles");

            return Ok(await _userManager.GetRolesAsync(user));

        }


        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-for-approval")]
        public async Task<ActionResult> GetPhotosForModeration()
        {
            var unapprovedPhotos = await _uow.PhotoRepository.GetUnapprovedPhotos();
            //var photoDtos = _mapper.Map<IEnumerable<PhotoDto>>(unapprovedPhotos);

            return Ok(unapprovedPhotos);
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("approve-photo/{photoId}")]
        public async Task<ActionResult> ApprovePhoto(int photoId)
        {

            var photo = await _uow.PhotoRepository.GetPhotoById(photoId);

            if (photo == null)
            {
                return NotFound("Photo not found");
            }

            photo.IsApproved = true;

            var user = await _uow.UserRepository.GetUserByPhotoId(photoId);

            if(user == null) return NotFound();

            if(!user.Photos.Any(x => x.IsMain)) photo.IsMain = true;

            await _uow.Complete();

            return Ok(new { message = "Photo Approved" });
        }



        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("reject-photo/{photoId}")]
        public async Task<IActionResult> RejectPhoto(int photoId)
        {

            var photo = await _uow.PhotoRepository.GetPhotoById(photoId);

            if (photo.PublicId == null)
            {
                return NotFound("Photo not found");
            }

            var result = await _photoService.DeletePhotoAsync(photo.PublicId);

            if(result.Result == "ok")
            {
                 await _uow.PhotoRepository.RemovePhoto(photo);

            }
            else
            {
                await _uow.PhotoRepository.RemovePhoto(photo);
            }

            await _uow.Complete();

            return Ok(new { message = "Photo rejected" });
        }
    }
}



    
