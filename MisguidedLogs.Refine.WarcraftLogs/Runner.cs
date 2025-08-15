using BunnyCDN.Net.Storage.Models;
using MisguidedLogs.Refine.WarcraftLogs.Bunnycdn;
using MisguidedLogs.Refine.WarcraftLogs.Mappers;
using MisguidedLogs.Refine.WarcraftLogs.Model;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MisguidedLogs.Refine.WarcraftLogs;

public class Runner(BunnyCdnStorageLoader loader, BunnyCdnStorageUploader uploader, Mapper mapper, ILogger<Runner> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            log.LogInformation("Retrieving storageObjects");
            var storageObjects = await loader.GetListOfStorageObjects("misguided-logs-warcraftlogs/reports/");
            
            log.LogInformation("Retrieving Newest Report");
            var newest = NewestReport(storageObjects);
            var detailsInfo = DetailsAssociatedWithReport(storageObjects, newest);
            var damageTaken = DamageTakenAssociatedWithReport(storageObjects, newest);
            var fightDeta = FightDetailsWithReport(storageObjects, newest);
            var report = await loader.GetStorageObject<ReportsResponse>(newest) ?? throw new ArgumentException("Failed to parse");
            var details = await loader.GetStorageObject<DetailsResponse>(detailsInfo) ?? throw new ArgumentException("Failed to parse");
            var dmgTaken = await loader.GetStorageObject<TablesInfoResponse>(damageTaken) ?? throw new ArgumentException("Failed to parse");
            var fightDetails = await loader.GetStorageObject<FightReportsResponse>(fightDeta) ?? throw new ArgumentException("Failed to parse");

            log.LogInformation("Mapping Report Info");
            var zones = mapper.GetZones(report).ToHashSet();
            var bosses = mapper.GetBosses(report).ToHashSet();
            var fights = mapper.GetFights(report, fightDetails).ToHashSet();
            var (players, stats) = mapper.CreatePlayerInfo(report, details, dmgTaken);

            log.LogInformation("Uploading results to BunnyCDN");
            await uploader.Upload(zones, $"misguided-logs-warcraftlogs/silver/{newest.ObjectName.Replace(".json.gz", "")}/zones.json.gz", cancellationToken);
            await uploader.Upload(bosses, $"misguided-logs-warcraftlogs/silver/{newest.ObjectName.Replace(".json.gz", "")}/bosses.json.gz", cancellationToken);
            await uploader.Upload(fights, $"misguided-logs-warcraftlogs/silver/{newest.ObjectName.Replace(".json.gz", "")}/fights.json.gz", cancellationToken);
            await uploader.Upload(stats, $"misguided-logs-warcraftlogs/silver/{newest.ObjectName.Replace(".json.gz", "")}/stats.json.gz", cancellationToken);
            await uploader.Upload(players, $"misguided-logs-warcraftlogs/silver/{newest.ObjectName.Replace(".json.gz", "")}/players.json.gz", cancellationToken);
        }
        catch (Exception e)
        {

            log.LogInformation("Something Went Wrong, Shutting down, Error {Exception}", e);
            Environment.Exit(1);
        }
        log.LogInformation("Finishing Job");
        Environment.Exit(0);
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static StorageObject NewestReport(StorageObject[] storageObjects)
    {
        if (storageObjects.Length == 0)
        {
            throw new ArgumentException("Empty Folder");
        }

        if (storageObjects.Length == 1)
        {
            return storageObjects[0];
        }

        return storageObjects.Where(x => !x.ObjectName.Contains("details")).OrderBy(x => DateTime.ParseExact(x.ObjectName.Split("__")[0], "yyyy-MM-dd_HH-MM", CultureInfo.InvariantCulture)).Last();
    }
    private static StorageObject DetailsAssociatedWithReport(StorageObject[] storageObjects, StorageObject storageObject)
    {
        return storageObjects.First(x => x.ObjectName == $"{storageObject.ObjectName.Split(".json.gz")[0]}__details.json.gz");
    }
    private static StorageObject DamageTakenAssociatedWithReport(StorageObject[] storageObjects, StorageObject storageObject)
    {
        return storageObjects.First(x => x.ObjectName == $"{storageObject.ObjectName.Split(".json.gz")[0]}__damagetaken.json.gz");
    }
    private static StorageObject FightDetailsWithReport(StorageObject[] storageObjects, StorageObject storageObject)
    {
        return storageObjects.First(x => x.ObjectName == $"{storageObject.ObjectName.Split(".json.gz")[0]}__fightDetails.json.gz");
    }
}
