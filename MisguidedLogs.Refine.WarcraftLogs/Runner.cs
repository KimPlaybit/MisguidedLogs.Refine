using BunnyCDN.Net.Storage.Models;
using MisguidedLogs.Refine.WarcraftLogs.Bunnycdn;
using MisguidedLogs.Refine.WarcraftLogs.Mappers;
using MisguidedLogs.Refine.WarcraftLogs.Model;
using System.Globalization;

namespace MisguidedLogs.Refine.WarcraftLogs;

public class Runner(BunnyCdnStorageLoader loader, BunnyCdnStorageUploader uploader, Mapper mapper, ILogger<Runner> log) : IHostedService
{
    private record LastRun(string Name);
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            log.LogInformation("Retrieving storageObjects");
            var storageObjectsFolders = await loader.GetListOfStorageObjects($"misguided-logs-warcraftlogs/reports/");
            foreach (var folder in storageObjectsFolders)
            {
                var zone = int.Parse(folder.ObjectName);
                log.LogInformation("Checking last file ran");
                var last = await loader.GetStorageObject<LastRun>(Path.Combine($"misguided-logs-warcraftlogs/silverrunner/{zone}", "lastrun.json.gz"));
                log.LogInformation("Retrieving storageObjects");
                var storageObjects = await loader.GetListOfStorageObjects($"misguided-logs-warcraftlogs/reports/{zone}");
                
                log.LogInformation("Retrieving Newest Report");
                var newest = NewestReport(storageObjects);
                if (!storageObjects.Any() || last is not null && newest.ObjectName == last.Name)
                {
                    log.LogInformation("No new file to process for {ObjectName}, shutting down", folder.ObjectName);
                    continue;
                }

                var detailsInfo = DetailsAssociatedWithReport(storageObjects, newest);
                var damageTaken = DamageTakenAssociatedWithReport(storageObjects, newest);
                var fightDeta = FightDetailsWithReport(storageObjects, newest);
                var report = await loader.GetStorageObject<ReportsResponse>(newest) ?? throw new ArgumentException("Failed to parse");
                var details = await loader.GetStorageObject<DetailsResponse>(detailsInfo) ?? throw new ArgumentException("Failed to parse");
                var dmgTaken = await loader.GetStorageObject<TablesInfoResponse>(damageTaken) ?? throw new ArgumentException("Failed to parse");
                var fightDetails = await loader.GetStorageObject<FightReportsResponse>(fightDeta) ?? throw new ArgumentException("Failed to parse");

                log.LogInformation("Mapping Report Info");
                var zones = mapper.GetZones(report).Where(x => x.Id == zone).ToHashSet();
                var bosses = mapper.GetBosses(report).Where(x => x.ZoneId == zone).ToHashSet();
                var fights = mapper.GetFights(report, fightDetails).ToHashSet();
                var (players, stats) = mapper.CreatePlayerInfo(report, details, dmgTaken);

                log.LogInformation("Uploading results to BunnyCDN");
                await uploader.Upload(zones, $"misguided-logs-warcraftlogs/silver/{zone}/{newest.ObjectName.Replace(".json.gz", "")}/zones.json.gz", cancellationToken);
                await uploader.Upload(bosses, $"misguided-logs-warcraftlogs/silver/{zone}/{newest.ObjectName.Replace(".json.gz", "")}/bosses.json.gz", cancellationToken);
                await uploader.Upload(fights, $"misguided-logs-warcraftlogs/silver/{zone}/{newest.ObjectName.Replace(".json.gz", "")}/fights.json.gz", cancellationToken);
                await uploader.Upload(stats, $"misguided-logs-warcraftlogs/silver/{zone}/{newest.ObjectName.Replace(".json.gz", "")}/stats.json.gz", cancellationToken);
                await uploader.Upload(players, $"misguided-logs-warcraftlogs/silver/{zone}/{newest.ObjectName.Replace(".json.gz", "")}/players.json.gz", cancellationToken);
                await uploader.Upload(new LastRun(newest.ObjectName), Path.Combine($"misguided-logs-warcraftlogs/silverrunner/{zone}", "lastrun.json.gz"), cancellationToken);

            }

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

        return storageObjects.Where(x => !x.ObjectName.Contains("details")).OrderBy(x => DateTime.ParseExact(x.ObjectName.Split("__")[0], "yyyy-MM-dd_HH-mm", CultureInfo.InvariantCulture)).Last();
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
