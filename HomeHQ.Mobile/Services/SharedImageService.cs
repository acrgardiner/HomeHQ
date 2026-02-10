using System;
using System.Collections.Generic;
using System.Text;

namespace HomeHQ.Mobile.Services;

public class SharedImageService
{
    /// <summary>
    /// The path to the pending shared image file (copied to app cache).
    /// </summary>
    public string? PendingImagePath { get; private set; }

    /// <summary>
    /// The original filename of the shared image.
    /// </summary>
    public string? PendingImageFileName { get; private set; }

    /// <summary>
    /// The content type of the shared image.
    /// </summary>
    public string? PendingImageContentType { get; private set; }

    /// <summary>
    /// Event raised when a new image is received via share intent.
    /// </summary>
    public event EventHandler? ImageReceived;

    /// <summary>
    /// Sets a pending image from a share intent.
    /// </summary>
    /// <param name="imagePath">Path to the cached image file</param>
    /// <param name="fileName">Original filename</param>
    /// <param name="contentType">MIME content type</param>
    public void SetPendingImage(string imagePath, string? fileName = null, string? contentType = null)
    {
        PendingImagePath = imagePath;
        PendingImageFileName = fileName ?? Path.GetFileName(imagePath);
        PendingImageContentType = contentType ?? GetContentTypeFromExtension(imagePath);
        ImageReceived?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears the pending image after it has been processed.
    /// </summary>
    public void ClearPendingImage()
    {
        // Optionally delete the cached file
        if (!string.IsNullOrEmpty(PendingImagePath) && File.Exists(PendingImagePath))
        {
            try
            {
                File.Delete(PendingImagePath);
            }
            catch
            {
                // Ignore deletion errors - cache will be cleaned up eventually
            }
        }

        PendingImagePath = null;
        PendingImageFileName = null;
        PendingImageContentType = null;
    }

    /// <summary>
    /// Returns true if there is a pending image waiting to be processed.
    /// </summary>
    public bool HasPendingImage => !string.IsNullOrEmpty(PendingImagePath) && File.Exists(PendingImagePath);

    /// <summary>
    /// Gets the file bytes of the pending image.
    /// </summary>
    public byte[]? GetPendingImageBytes()
    {
        if (!HasPendingImage) return null;

        try
        {
            return File.ReadAllBytes(PendingImagePath!);
        }
        catch
        {
            return null;
        }
    }

    private static string GetContentTypeFromExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".heic" => "image/heic",
            _ => "application/octet-stream"
        };
    }
}
