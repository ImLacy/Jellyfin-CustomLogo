using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using MediaBrowser.Common.Configuration;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CustomLogo;

[Route("logo")]
public class WebController : ControllerBase
{
    private const string IconTransparentPattern = "icon-transparent*";
    private const string TouchIconPattern = "touchicon*";
    private const string FaviconPattern = "favicon*";
    private const string BannerDarkPattern = "banner-dark*";
    private const string BannerLightPattern = "banner-light*";
    private readonly IApplicationPaths _appPaths;
    private readonly ILogger<WebController> _logger;

    public WebController(
        IApplicationPaths appPaths,
        ILogger<WebController> logger)
    {
        _appPaths = appPaths;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadLogo()
    {
        IFormFile? logo = Request.Form.Files["Logo"];
        IFormFile? bannerDark = Request.Form.Files["BannerDark"];
        IFormFile? bannerLight = Request.Form.Files["BannerLight"];

        try
        {
            await ReplaceFiles(logo, IconTransparentPattern, TouchIconPattern, FaviconPattern);
            await ReplaceFiles(bannerDark, BannerDarkPattern);
            await ReplaceFiles(bannerLight, BannerLightPattern);
        }
        catch (UnauthorizedAccessException)
        {
            return Content(
                "<html><head></head><body>Jellyfin does not have write access. Please check the permissions and ensure the application has sufficient rights.<br><a href='/web/#/dashboard/plugins'>Retun to Jellyfin</a></body></html>",
                "text/html");
        }

        return Content(
            "<html><head><meta http-equiv='refresh' content='0;url=/web/#/dashboard/plugins' /></head><body>Redirection...</body></html>",
            "text/html");
    }

    private async Task ReplaceFiles(IFormFile? file, params string[] patterns)
    {
        if (file is null || file.Length == 0)
            return;

        string tempPath = Path.GetTempFileName();

        try
        {
            IReadOnlyCollection<string> files = patterns
                                                .SelectMany(pattern => Directory.GetFiles(_appPaths.WebPath, pattern, SearchOption.AllDirectories))
                                                .ToList();

            await using FileStream fileStream = await CreateAndOpenTempFile(file, tempPath);

            _logger.LogDebug("Replacing following Files: {Files}", string.Join("\r\n", files));

            foreach (string filePath in files)
            {
                _logger.LogDebug("Replacing file {FilePath}", filePath);

                await using FileStream stream = new(filePath, FileMode.Create);
                fileStream.Position = 0;
                await fileStream.CopyToAsync(stream);

                _logger.LogDebug("Replaced file {FilePath} successful", filePath);
            }
        }
        finally
        {
            System.IO.File.Delete(tempPath);
        }
    }

    private static async Task<FileStream> CreateAndOpenTempFile(IFormFile file, string tempPath)
    {
        await using (FileStream fileStream = System.IO.File.Create(tempPath))
            await file.CopyToAsync(fileStream);

        return System.IO.File.OpenRead(tempPath);
    }
}