namespace MisguidedLogs.Refine.WarcraftLogs.Model;

public record FightReportsResponse(FightReportData ReportData);
public record FightReportData(FightReports Reports);
public record FightReports(FightReport[] Data);
public record FightReport(string Code, long StartTime, FightDetails[] Fights);
public record FightDetails(int Id, long StartTime, long EndTime, float? BossPercentage)
{
    public string Code { get; set; } = "";
    public long CompleteEndTime { get; set; } = 0;
    public long CompleteStartTime { get; set; } = 0;
}