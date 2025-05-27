using GoogleMapLinkChecker.Services;
using GoogleMapLinkChecker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GoogleMapLinkChecker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinkCheckerController : ControllerBase
    {
        private readonly ILinkChecker _linkChecker;

        public LinkCheckerController(ILinkChecker linkChecker)
        {
            _linkChecker = linkChecker;
        }

        [HttpGet("validate")]
        public async Task<IActionResult> CheckLink([FromQuery] string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                return BadRequest("Link cannot be null or empty.");
            }
            if (!_linkChecker.IsUrlValidAsync(link))
            {
                return BadRequest("Invalid URL format.");
            }
            bool exists = await _linkChecker.UrlExistAsync(link);
            if (!exists)
            {
                return NotFound("URL does not exist.");
            }

            PageInfo? content = await _linkChecker.GetUrlContentAsync(link);

            if (!_linkChecker.IsGoogleMapsLink(link))
            {
                return BadRequest(new {Note = "The link is not a valid Google Maps link.", Content = content});
            }
            else
            {
                return Ok(new { Note = "The link is a valid Google Maps link.", Content = content });
            }
        }
    }
}
