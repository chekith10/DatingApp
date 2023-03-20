using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class BuggyController : BaseApiController
    {
    private readonly DataContext __context;
     public BuggyController(DataContext _context)
     {
            __context = _context;
        
     }

        [Authorize]
        [HttpGet("auth")]
        public ActionResult<string> GetSecret()
        {
            return "secret-text";
        }

        [HttpGet("server-error")]
        public ActionResult<string> GetServerError()
        {
            var thing = __context.Users.Find(-1);

            var thingToReturn = thing.ToString();

            return thingToReturn;
        }
        
        [HttpGet("not-found")]
        public ActionResult<AppUser> GetNotFound()
        {
           var thing = __context.Users.Find(-1);

           if(thing == null) return NotFound();

           return thing;
        }
        [HttpGet("bad-request")]
        public ActionResult<string> GetBadRequest()
        {
            return BadRequest("This was not a good request");
        }

    }
}