﻿using BunnyCDN.Net.Storage;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MisguidedLogs.Refine.WarcraftLogs.Bunnycdn;

public class BunnyCdnStorageUploader(BunnyCDNStorage storage)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false, Converters = { new JsonStringEnumConverter() } };

    public async Task Upload<T>(T content, string filePath, CancellationToken cancellationToken)
    {
        var serializedContent = JsonSerializer.SerializeToUtf8Bytes(content, JsonOptions);
        using var gzipStream = new MemoryStream(await Compress(serializedContent));
        await storage.UploadAsync(gzipStream, filePath);
    }
    private static async Task<byte[]> Compress(byte[] bytes)
    {
        using var memoryStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
        {
            await gzipStream.WriteAsync(bytes);
        }
        return memoryStream.ToArray();
    }
}
