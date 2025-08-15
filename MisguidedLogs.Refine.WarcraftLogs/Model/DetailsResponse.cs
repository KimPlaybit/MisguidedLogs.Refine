namespace MisguidedLogs.Refine.WarcraftLogs.Model;

public record DetailsResponse(DetailData ReportData);
public record DetailData(DetailReports Reports);
public record DetailReports(DetailReport[] Data);
public record DetailReport(string Code, PlayerDetailsData PlayerDetails, EventsResponse Events);
public record PlayerDetailsData(Data Data);
public record Data(PlayerDetails PlayerDetails);
public record PlayerDetails(List<PlayerDetail> Dps, List<PlayerDetail> Tanks, List<PlayerDetail> Healers);

public record PlayerDetail(
    string Name,
    int Id,
    long Guid,
    string Type,
    string Server,
    string Region,
    string Icon,
    List<TalentSpec> Specs,
    int MinItemLevel,
    int MaxItemLevel,
    int PotionUse,
    int HealthstoneUse
);

public record TalentSpec(string Spec, int Count);
public record EventsResponse(List<Event> Data);

public record Event(
    int Timestamp,
    string Type,
    int Fight,
    int SourceID,
    string Error,
    string Expansion,
    int Strength,
    int Agility,
    int Stamina,
    int Intellect,
    int Spirit,
    int Dodge,
    int Parry,
    int Block,
    int Armor,
    List<Talent> Talents
);

public record Talent(int Id, string Icon);