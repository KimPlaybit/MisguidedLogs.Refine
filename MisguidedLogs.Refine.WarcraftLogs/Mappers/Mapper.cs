using MisguidedLogs.Refine.WarcraftLogs.Model;
using MisguidedLogs.Refine.WarcraftLogs.Model.Result;
using System.Security.Cryptography;
using System.Text;

namespace MisguidedLogs.Refine.WarcraftLogs.Mappers;

public class Mapper
{
    public Zone[] GetZones(ReportsResponse intro)
    {
        return [.. intro.ReportData.Reports.Data.SelectMany(x => x.Dps.Data.Select(x => new Zone(x.Zone)))];
    }

    public Boss[] GetBosses(ReportsResponse intro)
    {
        return [.. intro.ReportData.Reports.Data.SelectMany(x => x.Dps.Data.Select(y => new Boss(y.Encounter.Id, y.Encounter.Name, y.Zone)))];
    }

    public Model.Result.Fight[] GetFights(ReportsResponse intro, FightReportsResponse fightReportsResponse)
    {
        var fightDetails = fightReportsResponse.ReportData.Reports.Data.SelectMany(x => x.Fights.Select(y =>
        {
            y.CompleteEndTime = x.StartTime + y.EndTime;
            y.CompleteStartTime = x.StartTime + y.StartTime;
            y.Code = x.Code;
            return y;
        }));

        return [.. intro.ReportData.Reports.Data.SelectMany(x => x.Dps.Data.Where(y => y.Zone != 2510).Select(y => CreateFight(x.Code, y, fightDetails)).Where(x => x is not null))!];
    }

    private static Model.Result.Fight? CreateFight(string code, Model.Fight fight, IEnumerable<FightDetails> fightDetails)
    {

        try
        {
            var connectedFight = fightDetails.FirstOrDefault(x => x.Code == code && x.Id == fight.FightId);
            if (connectedFight is null)
            {
                return null;
            }
            return new Model.Result.Fight($"{fight.Encounter.Id}_{code}_{fight.FightId}", fight.Encounter.Id, DateTimeOffset.FromUnixTimeMilliseconds(connectedFight.CompleteStartTime).UtcDateTime, DateTimeOffset.FromUnixTimeMilliseconds(connectedFight.CompleteEndTime).UtcDateTime);

        }
        catch (Exception e)
        {

            throw;
        }
    }

    public (Model.Result.Player[] Players, PlayerStats[] Stats) CreatePlayerInfo(ReportsResponse reports, DetailsResponse details, TablesInfoResponse damageTakenInfo)
    {
        var allInfo = reports.ReportData.Reports.Data.Select(x => GetPlayersInfo(x, details, damageTakenInfo));

        return ([.. allInfo.SelectMany(x => x.players)], [.. allInfo.SelectMany(x => x.stats)]);
    }
    private (Model.Result.Player[] players, PlayerStats[] stats) GetPlayersInfo(Report report, DetailsResponse details, TablesInfoResponse damageTakenInfo)
    {
        var code = report.Code;
        var detailForReport = details.ReportData.Reports.Data.First(x => x.Code == code);
        var dmgTakenInfo = damageTakenInfo.ReportData.Reports.Data.First(x => x.Code == code);

        var tankDpsInfo = report.Dps.Data.SelectMany(x => CreateGroupedPlayer(x.Roles.Tanks.Characters, code, x.FightId, x.Encounter));
        var tankHpsInfo = report.Hps.Data.SelectMany(x => CreateGroupedPlayer(x.Roles.Tanks.Characters, code, x.FightId, x.Encounter));
        var tanks = CreatePlayersAndStats(tankDpsInfo, tankHpsInfo, detailForReport, dmgTakenInfo);

        var dpsDpsInfo = report.Dps.Data.SelectMany(x => CreateGroupedPlayer(x.Roles.Dps.Characters, code, x.FightId, x.Encounter));
        var dpsHpsInfo = report.Hps.Data.SelectMany(x => CreateGroupedPlayer(x.Roles.Dps.Characters, code, x.FightId, x.Encounter));
        var dps = CreatePlayersAndStats(dpsDpsInfo, dpsHpsInfo, detailForReport, dmgTakenInfo);

        var hpsDpsInfo = report.Dps.Data.SelectMany(x => CreateGroupedPlayer(x.Roles.Healers.Characters, code, x.FightId, x.Encounter));
        var hpsHpsInfo = report.Hps.Data.SelectMany(x => CreateGroupedPlayer(x.Roles.Healers.Characters, code, x.FightId, x.Encounter));
        var hps = CreatePlayersAndStats(hpsDpsInfo, hpsHpsInfo, detailForReport, dmgTakenInfo);

        return ([.. tanks.players.Union(dps.players).Union(hps.players).ToHashSet()], [.. tanks.stats.Union(dps.stats).Union(hps.stats)]);
    }

    private static IEnumerable<IGrouping<Key, PlayerInfo>> CreateGroupedPlayer(Model.Player[] players, string code, int fightId, Encounter encounter)
    {
        return players.Select(c => new PlayerInfo(c, encounter, fightId, code)).GroupBy(x => new Key(x.player.Name, x.player.Server, x.FightId));
    }


    private static (Model.Result.Player[] players, PlayerStats[] stats) CreatePlayersAndStats(IEnumerable<IGrouping<Key, PlayerInfo>> tanksDps, IEnumerable<IGrouping<Key, PlayerInfo>> tanksHps, DetailReport detailReport, TableResponse damageTakenInfo)
    {
        var players = new List<Model.Result.Player>();
        var result = new List<PlayerStats>();
        foreach (var dps in tanksDps.Where(x => x.Key.FightId < 1000))
        {
            var playerDetails = GetPlayerDetail(detailReport.PlayerDetails.Data.PlayerDetails.Dps, dps.Key.Name, dps.Key.Server.Name) ??
                    GetPlayerDetail(detailReport.PlayerDetails.Data.PlayerDetails.Tanks, dps.Key.Name, dps.Key.Server.Name) ??
                    GetPlayerDetail(detailReport.PlayerDetails.Data.PlayerDetails.Healers, dps.Key.Name, dps.Key.Server.Name);

            var detailInfo = detailReport.Events.Data.FirstOrDefault(x => x.Fight == dps.Key.FightId && x.SourceID == playerDetails.Id);

            if (detailInfo is null)
            {
                continue;
            }

            var playerDmgTaken = GetEntry(damageTakenInfo, detailInfo.SourceID, detailInfo.Fight, playerDetails.Guid);

            var hps = tanksHps.FirstOrDefault(x => x.Key == dps.Key);

            var playerInfo = CreatePlayerStats(dps, hps, playerDetails, detailInfo, playerDmgTaken);
            players.Add(playerInfo.player);
            result.AddRange(playerInfo.stats);
        }

        foreach (var hps in tanksHps.Where(x => x.Key.FightId < 1000 && !tanksDps.Any(y => y.Key == x.Key)))
        {
            var playerDetails = GetPlayerDetail(detailReport.PlayerDetails.Data.PlayerDetails.Dps, hps.Key.Name, hps.Key.Server.Name) ??
                    GetPlayerDetail(detailReport.PlayerDetails.Data.PlayerDetails.Tanks, hps.Key.Name, hps.Key.Server.Name) ??
                    GetPlayerDetail(detailReport.PlayerDetails.Data.PlayerDetails.Healers, hps.Key.Name, hps.Key.Server.Name);

            var detailInfo = detailReport.Events.Data.FirstOrDefault(x => x.Fight == hps.Key.FightId && x.SourceID == playerDetails.Id);

            if (detailInfo is null)
            {
                continue;
            }

            var playerDmgTaken = GetEntry(damageTakenInfo, detailInfo.SourceID, detailInfo.Fight, playerDetails!.Guid);

            var playerInfo = CreatePlayerStats(null, hps, playerDetails, detailInfo, playerDmgTaken);
            players.Add(playerInfo.player);
            result.AddRange(playerInfo.stats);
        }
        return ([.. players], [.. result]);

    }

    private static Entry? GetEntry(TableResponse damageTakenInfo, int playerId, int fightId, long playerGuId)
    {
        return damageTakenInfo.Tables[fightId].Data.Entries.FirstOrDefault(x => x.Id == playerId);
    }

    private static PlayerDetail? GetPlayerDetail(List<PlayerDetail> details, string name, string serverName)
    {
        var player = details.FirstOrDefault(x => x.Name == name);
        return details.FirstOrDefault(x => x.Server.Replace(" ", "") == serverName.Replace(" ", "") && x.Name == name);
    }

    private static (Model.Result.Player player, PlayerStats[] stats) CreatePlayerStats(
        IGrouping<Key, PlayerInfo>? dps,
        IGrouping<Key, PlayerInfo>? hps,
        PlayerDetail playerDetails,
        Event detailInfo,
        Entry? entry)
    {
        var playerKey = dps?.Key ?? hps!.Key;
        var playerStats = new List<PlayerStats>();

        var uniqueGuid = CreateUniqueGuid(playerKey.Name, playerKey.Server.Name, playerKey.Server.Region);

        if (dps is not null)
        {
            foreach (var dpsInfo in dps)
            {
                var playerStatId = $"{dpsInfo.Encounter.Id}_{dpsInfo.Code}_{dpsInfo.FightId}";

                var bossDmg = entry?.Sources.FirstOrDefault(x => x.Name == dpsInfo.Encounter.Name)?.Total ?? 0;
                var meleeDmgTaken = entry?.Abilities.Where(x => x.Name is "Melee").FirstOrDefault()?.Total ?? 0;

                var hpsInfo = hps?.FirstOrDefault(x => x.Encounter == dpsInfo.Encounter) ?? null;
                playerStats.Add(new PlayerStats(
                    uniqueGuid,
                    playerStatId,
                    hpsInfo?.player.Amount ?? 0,
                    dpsInfo.player.Amount,
                    entry?.Total ?? 0,
                    bossDmg,
                    meleeDmgTaken,
                    new Specialization(SpecMapper.GetClassSpecc(dpsInfo.player.Class, detailInfo.Talents), detailInfo.Talents[0].Id, detailInfo.Talents[1].Id, detailInfo.Talents[2].Id)));
            }
        }
        else
        {
            //this can most likely not occure, due to this is a case where players are not in the dpsList
            foreach (var healerInfo in hps!)
            {
                var playerStatId = $"{healerInfo.Encounter.Id}_{healerInfo.Code}_{healerInfo.FightId}";

                var bossDmg = entry?.Sources.FirstOrDefault(x => x.Name == healerInfo.Encounter.Name)?.Total ?? 0;
                var meleeDmgTaken = entry?.Abilities.Where(x => x.Name is "Melee").FirstOrDefault()?.Total ?? 0;

                playerStats.Add(new PlayerStats(
                    uniqueGuid,
                    playerStatId,
                    healerInfo?.player.Amount ?? 0,
                    0,
                    entry?.Total ?? 0,
                    bossDmg,
                    meleeDmgTaken,
                    new Specialization(SpecMapper.GetClassSpecc(healerInfo!.player.Class, detailInfo.Talents), detailInfo.Talents[0].Id, detailInfo.Talents[1].Id, detailInfo.Talents[2].Id)));
            }
        }

        var playerInfo = dps?.First() ?? hps?.First();
        var player = new Model.Result.Player(uniqueGuid, entry?.Guid ?? 0, dps.Key.Name, dps.Key.Server.Name, dps.Key.Server.Region, playerInfo.player.Class);

        return (player, [.. playerStats]);
    }

    private static string CreateUniqueGuid(string name, string server, string region)
    {
        // Combine the strings
        var combinedString = $"{name}-{server}-{region}";

        // Compute the SHA256 hash
        using (var md5 = MD5.Create())
        {
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(combinedString));

            // Convert the byte array to a hexadecimal string
            var hexString = new StringBuilder(hashBytes.Length * 2);
            foreach (byte b in hashBytes)
            {
                hexString.AppendFormat("{0:x2}", b);
            }

            return hexString.ToString();
        }
    }

    private record Key(string Name, Server Server, int FightId);
    private record PlayerInfo(Model.Player player, Encounter Encounter, int FightId, string Code);
}