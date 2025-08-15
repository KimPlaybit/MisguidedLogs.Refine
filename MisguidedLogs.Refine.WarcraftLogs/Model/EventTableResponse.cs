namespace MisguidedLogs.Refine.WarcraftLogs.Model;

public record TablesInfoResponse(TableData ReportData);
public record TableData(TableReports Reports);
public record TableReports(TableResponse[] Data);
public record TableResponse(string Code, Dictionary<int, TableValue> Tables);
public record TableValue(
    Table Data
    );
public record Table(
    Entry[] Entries,
    int TotalTime,
    int LogVersion,
    int GameVersion
);

public record Entry(
    string Name, 
    int Id, 
    int Guid,
    int ItemLevel,
    int Total,
    int TotalReduced,
    long ActiveTime,
    long ActiveTimeReduced,
    int OverHeal,
    Ability[] Abilities,
    DamageSource[] Sources
    );

public record Ability(
    string Name,
    int Total,
    int TotalReduced,
    int Type
    );

public record DamageSource(
    string Name,
    int Total,
    int TotalReduced,
    string Type
    );