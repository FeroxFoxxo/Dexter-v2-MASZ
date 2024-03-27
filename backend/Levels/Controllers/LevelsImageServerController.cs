using Bot.Abstractions;
using Bot.Exceptions;
using Bot.Models;
using Levels.Data;
using Microsoft.AspNetCore.Mvc;

namespace Levels.Controllers;

[Route("api/v1/levels")]
public class LevelsImageServerController(LevelsImageRepository levelsImageRepository) : BaseController
{
    private readonly LevelsImageRepository _levelsImageRepository = levelsImageRepository;

    [HttpGet("{userId}/images/{fileName}")]
    public async Task<IActionResult> GetImage([FromRoute] ulong userId, [FromRoute] string fileName)
    {
        UploadedFile fileInfo;
        try
        {
            fileInfo = await _levelsImageRepository.GetUserFile(userId, fileName);
        }
        catch (ResourceNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        return SendImage(fileInfo);
    }

    [HttpGet("default/images/{fileName}")]
    public IActionResult GetDefaultImage([FromRoute] string fileName)
    {
        UploadedFile fileInfo;
        try
        {
            fileInfo = _levelsImageRepository.GetDefaultFile(fileName);
        }
        catch (ResourceNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        return SendImage(fileInfo);
    }

    private FileContentResult SendImage(UploadedFile fileInfo)
    {
        HttpContext.Response.Headers.ContentType = fileInfo.ContentType;
        HttpContext.Response.Headers.ContentDisposition = fileInfo.ContentDisposition.ToString();

        return File(fileInfo.FileContent, fileInfo.ContentType);
    }

    [HttpGet("default/images")]
    public IActionResult GetDefaultImages() => Ok(LevelsImageRepository.GetDefaultFiles());
}
