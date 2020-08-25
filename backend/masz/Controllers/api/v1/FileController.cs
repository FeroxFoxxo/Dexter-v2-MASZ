using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using masz.data;
using masz.Dtos.ModCase;
using masz.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace masz.Controllers
{
    [ApiController]
    [Route("api/v1/files/{guildid}/{modcaseid}/")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> logger;
        private readonly IAuthRepository authRepo;
        private readonly DataContext dbContext;
        private readonly IDiscordRepository discordRepo;
        private readonly IOptions<InternalConfig> config;

        public FileController(ILogger<FileController> logger, IAuthRepository authRepo, DataContext context, IDiscordRepository discordRepo, IOptions<InternalConfig> config)
        {
            this.logger = logger;
            this.authRepo = authRepo;
            this.dbContext = context;
            this.discordRepo = discordRepo;
            this.config = config;
        }

        [HttpGet("{filename}")]
        public async Task<IActionResult> GetSpecificItem([FromRoute] string guildid, [FromRoute] string modcaseid, [FromRoute] string filename) 
        {
            logger.LogInformation($"{HttpContext.Request.Method} {HttpContext.Request.Path} | Incoming request.");
            if (! await authRepo.DiscordUserHasModRoleOrHigherOnGuild(HttpContext, guildid))
            {
                logger.LogInformation($"{HttpContext.Request.Method} {HttpContext.Request.Path} | 401 Unauthorized.");
                return Unauthorized();
            }

            var uploadDir = Path.Combine(config.Value.AbsolutePathToFileUpload , guildid, modcaseid);
            var filePath = Path.Combine(uploadDir, filename);
            if (!System.IO.File.Exists(filePath))
            {
                logger.LogInformation($"{HttpContext.Request.Method} {HttpContext.Request.Path} | 404 Not Found.");
                return NotFound();
            }

            byte[] filedata = System.IO.File.ReadAllBytes(filePath);
            string contentType;
            new FileExtensionContentTypeProvider().TryGetContentType(filePath, out contentType);
            contentType = contentType ?? "application/octet-stream";
            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = filename,
                Inline = true,
            };
            HttpContext.Response.Headers.Add("Content-Disposition", cd.ToString());
            HttpContext.Response.Headers.Add("Content-Type", contentType);

            return File(filedata, contentType);
        }

        [HttpPost]
        [RequestSizeLimit(10485760)]
        public async Task<IActionResult> PostItem([FromRoute] string guildid, [FromRoute] string modcaseid, [FromForm] UploadedFile uploadedFile)
        {
            logger.LogInformation($"{HttpContext.Request.Method} {HttpContext.Request.Path} | Incoming request.");
            if (! await authRepo.DiscordUserHasModRoleOrHigherOnGuild(HttpContext, guildid))
            {
                logger.LogInformation($"{HttpContext.Request.Method} {HttpContext.Request.Path} | 401 Unauthorized.");
                return Unauthorized();
            }

            if (uploadedFile.File == null)
            {
                logger.LogInformation($"{HttpContext.Request.Method} {HttpContext.Request.Path} | 400 No file provided.");
                return BadRequest();
            }

            var uniqueFileName = GetUniqueFileName(uploadedFile.File);
            var uploadDir = Path.Combine(config.Value.AbsolutePathToFileUpload , guildid, modcaseid);
            System.IO.Directory.CreateDirectory(uploadDir);
            var filePath = Path.Combine(uploadDir, uniqueFileName);
            await uploadedFile.File.CopyToAsync(new FileStream(filePath, FileMode.Create));

            return StatusCode(201, new { path = $"/{guildid}/{modcaseid}/{uniqueFileName}" });
        }

        private string GetUniqueFileName(IFormFile file)
        {
            // TODO: change to hasing algorithm
            string fileName = Path.GetFileName(file.FileName);
            return  GetSHA1Hash(file)
                    + "_"
                    + Guid.NewGuid().ToString().Substring(0, 8)
                    + "_"
                    + Path.GetFileNameWithoutExtension(fileName)
                    + Path.GetExtension(fileName);
        }

        private string GetSHA1Hash(IFormFile file)
        {
            // get stream from file then convert it to a MemoryStream
            MemoryStream stream = new MemoryStream();
            file.OpenReadStream().CopyTo(stream);
            // compute md5 hash of the file's byte array.
            byte[] bytes = SHA1.Create().ComputeHash(stream.ToArray());
            stream.Close();
            return BitConverter.ToString(bytes).Replace("-",string.Empty).ToLower();
        }
    }
}