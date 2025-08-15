using BunnyCDN.Net.Storage;
using BunnyCDN.Net.Storage.Models;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MisguidedLogs.Refine.WarcraftLogs.Bunnycdn;

public class BunnyCdnStorageLoader(BunnyCDNStorage bunnyCDNStorage)
{
    public async Task<StorageObject[]> GetListOfStorageObjects(string path)
    {
        return [.. await bunnyCDNStorage.GetStorageObjectsAsync(path)];
    }
    
    public async Task<T?> GetStorageObject<T>(StorageObject storageObject)
    {
        var stream = await bunnyCDNStorage.DownloadObjectAsStreamAsync(storageObject.FullPath);
        await using var decompressStream = new GZipStream(stream, CompressionMode.Decompress);
        return JsonSerializer.Deserialize<T>(decompressStream, new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } }) ?? default;
    }
}
