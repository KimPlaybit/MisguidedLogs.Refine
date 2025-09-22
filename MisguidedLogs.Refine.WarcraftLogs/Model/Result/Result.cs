using MisguidedLogs.Refine.WarcraftLogs.Mappers;

namespace MisguidedLogs.Refine.WarcraftLogs.Model.Result;


public record Zone(int Id)
{
    public string Name => Zones.TryGetValue(Id, out var Zone) ? Zone : ""; 

    private static Dictionary<int, string> Zones = new Dictionary<int, string>
        {
            { 1036, "Naxxramas" },
            { 1035, "Temple of Ahn'Qiraj" },
            { 1031, "Ruins of Ahn'Qiraj" },
            { 1030, "Zul'Gurub" },
            { 1034, "Blackwing Lair" },
            { 1029, "Onyxia" },
            { 1028, "Molten Core" },
        };
}
public record Boss(int Id, string Name, int ZoneId);
public record Fight(string FightId, int BossId, DateTime StartTime, DateTime EndTime);

//The Id for the players stats is BossId, Code and FightId
public record PlayerStats(string PlayerId, string FightId, float Hps, float Dps, long TotalDamageTaken, long TotalDamageTakenByBoss, long MeleeDmgTaken, Specialization Spec);
public record Specialization(TalentSpec Spec, int FirstTree, int SecondTree, int ThirdTree);
public record Player(string PlayerId, int Guid, string Name, string Server, string Region, Class Class);
public enum TalentSpec
{
    Discipline,
    Holy,
    Shadow,
    Affliction,
    Demonlogy,
    Destruction,
    Arcane,
    Fire,
    Frost,
    Assasination,
    Combat,
    Subtlety,
    Balance,
    Feral,
    Restoration,
    BeastMastery,
    Marksmanship,
    Survival,
    Elemental,
    Enhancement,
    Protection,
    Retribution,
    Arms,
    Fury,
    Blood,
    Unholy,
    Hybrid
}

public record PlayerResults(
    Dictionary<Class, Player> Priests,
    Dictionary<Class, Player> Warlocks,
    Dictionary<Class, Player> Mages,
    Dictionary<Class, Player> Rogues,
    Dictionary<Class, Player> Druids,
    Dictionary<Class, Player> Hunters,
    Dictionary<Class, Player> Shamans,
    Dictionary<Class, Player> Paladins,
    Dictionary<Class, Player> Warriors
    );