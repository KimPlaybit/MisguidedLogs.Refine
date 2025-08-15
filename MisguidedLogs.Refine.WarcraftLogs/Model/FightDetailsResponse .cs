namespace MisguidedLogs.Refine.WarcraftLogs.Model;

public record ReportsResponse(ReportData ReportData);
public record ReportData(Reports Reports);
public record Reports(Report[] Data);
public record Report(string Code, Fights Dps, Fights Hps);
public record Fights(Fight[] Data);
public record Fight(int FightId, int Partition, int Zone, Encounter Encounter, Roles Roles);
public record Encounter(int Id, string Name);
public record Roles(Role Tanks, Role Healers, Role Dps);
public record Role(string Name, Player[] Characters);
public record Player(long Id, string Name, Server Server, Class Class, float Amount);
public record Server(int Id, string Name, string Region);
public enum Class
{
    Priest,
    Warlock,
    Mage,
    Rogue,
    Druid,
    Shaman,
    Hunter,
    Warrior,
    Paladin,
    Monk,
    DemonHunter,
    DeathKnight,
    Evoker
}