using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustomLogo;

public class LogoCopyService : IHostedService
{
    private readonly ILogger<LogoCopyService> _logger;
    private readonly IApplicationPaths _appPaths;
    private readonly string _logoDirectory;

    public LogoCopyService(ILogger<LogoCopyService> logger, IApplicationPaths appPaths)
    {
        _logger = logger;
        _appPaths = appPaths;
        _logoDirectory = Path.Combine(appPaths.PluginConfigurationsPath, "CustomLogo");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            CopyLogosToWebPath();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomLogo: Failed to copy logos to web path");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void CopyLogosToWebPath()
    {
        if (!Directory.Exists(_logoDirectory))
        {
            _logger.LogInformation("CustomLogo: No custom logo directory found, skipping copy");
            return;
        }

        var webPath = _appPaths.WebPath;
        if (string.IsNullOrEmpty(webPath) || !Directory.Exists(webPath))
        {
            _logger.LogWarning("CustomLogo: Web path not found: {WebPath}", webPath);
            return;
        }

        CopyLogoIfExists("icon-transparent.png", webPath);
        CopyLogoIfExists("banner-dark.png", webPath);
        CopyLogoIfExists("banner-light.png", webPath);
    }

    private void CopyLogoIfExists(string logoFileName, string webPath)
    {
        var customLogoPath = Path.Combine(_logoDirectory, logoFileName);
        if (!File.Exists(customLogoPath))
        {
            return;
        }

        var logoBaseName = Path.GetFileNameWithoutExtension(logoFileName);
        var logoExtension = Path.GetExtension(logoFileName);

        var matchingFiles = Directory.GetFiles(webPath, $"{logoBaseName}*{logoExtension}", SearchOption.AllDirectories)
            .Where(f => IsMatchingLogoFile(f, logoBaseName, logoExtension))
            .ToList();

        if (matchingFiles.Count == 0)
        {
            var assetsPath = Path.Combine(webPath, "assets", "img");
            if (Directory.Exists(assetsPath))
            {
                var assetsFiles = Directory.GetFiles(assetsPath, $"{logoBaseName}*{logoExtension}", SearchOption.TopDirectoryOnly)
                    .Where(f => IsMatchingLogoFile(f, logoBaseName, logoExtension))
                    .ToList();
                matchingFiles.AddRange(assetsFiles);
            }
        }

        foreach (var targetFile in matchingFiles)
        {
            try
            {
                File.Copy(customLogoPath, targetFile, overwrite: true);
                _logger.LogInformation("CustomLogo: Copied {Source} to {Target}", logoFileName, targetFile);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "CustomLogo: No write permission for {Target}", targetFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CustomLogo: Failed to copy {Source} to {Target}", logoFileName, targetFile);
            }
        }

        if (matchingFiles.Count == 0)
        {
            _logger.LogDebug("CustomLogo: No matching files found for {Logo} in web path", logoFileName);
        }
    }

    private static bool IsMatchingLogoFile(string filePath, string baseName, string extension)
    {
        var fileName = Path.GetFileName(filePath);

        if (fileName.Equals($"{baseName}{extension}", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (fileName.StartsWith(baseName + ".", StringComparison.OrdinalIgnoreCase) &&
            fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            var middle = fileName.Substring(baseName.Length + 1, fileName.Length - baseName.Length - 1 - extension.Length);
            return !string.IsNullOrEmpty(middle) && middle.All(c => char.IsLetterOrDigit(c));
        }

        return false;
    }
}
