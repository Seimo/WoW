using ArgentPonyWarcraftClient;
using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;
using Guild = WoWArmory.Contracts.Models.Guild;

namespace WoWArmory.Services;

public class RaidLogService(
    RaidService raidService,
    ArmoryDbContext dbContext,
    ArmoryApiClient warcraftClientApi,
    WarcraftLogsApiClient warcraftLogsClientApi)
    : BaseService(dbContext, warcraftClientApi, warcraftLogsClientApi)
{
    
    private RaidService RaidService { get; } = raidService;
    
    public IQueryable<RaidLog> GetRaidLogs()
    {
        return DbContext.RaidLogs;
    }

    public RaidLog? GetRaidLog(Guid id)
    {
        return GetRaidLogs().FirstOrDefault(rl => rl.Id == id);
    }

    public async Task<RaidLog?> GetRaidLogAsync(Guid id)
    {
        return await GetRaidLogs().FirstOrDefaultAsync(rl => rl.Id == id);
    }

    public IQueryable<RaidLog> GetRaidLogs(Guid raidId)
    {
        return DbContext.RaidLogs
            .Where(rl => rl.Raid.Id == raidId);
    }

    public async Task<RaidLog?> AddRaidLog(Guid raidId, string reportCode)
    {
        var raidTask = RaidService.GetRaidAsync(raidId);
        if (raidTask.Result == null) return null;

        var raid = raidTask.Result;
        var dbRaidLog = GetRaidLogs().FirstOrDefault(rl => rl.Raid == raid && rl.WarcraftLogsCode == reportCode);
        if (dbRaidLog == null)
        {
            var warcraftLogsClient = GetWarcraftLogsClient();
            dbRaidLog = new RaidLog
            {
                Raid = raid,
                WarcraftLogsCode = reportCode,
                Name = $"{raid.Name} - Raid",
                Url = warcraftLogsClient.GetReportsUri(reportCode),
                Date = DateTime.UtcNow
            };
            DbContext.RaidLogs.Add(dbRaidLog);
        }
        
        UpdateRaidLog(dbRaidLog);
        await SaveChangesAsync();

        return dbRaidLog;
    }

    public async Task RemoveRaidLog(Guid id)
    {
        var raidLog = await GetRaidLogAsync(id);
        if (raidLog == null) return;

        DbContext.RaidLogs.Remove(raidLog);
        await SaveChangesAsync();
    }

    public IEnumerable<RaidLog> UpdateRaidLogs()
    {
        var raidLogs = GetRaidLogs();

        foreach (var raidLog in raidLogs)
            UpdateRaidLog(raidLog);
        SaveChanges();
        
        return raidLogs;
    }

    public async Task<RaidLog?> UpdateRaidLog(Guid id)
    {
      var raidLog = await GetRaidLogAsync(id);
      
      if (raidLog == null) return null;
      UpdateRaidLog(raidLog);
      await SaveChangesAsync();
      return raidLog;
    }

    private void UpdateRaidLog(RaidLog raidLog)
    {
        try
        {
            var warcraftLogsClient = GetWarcraftLogsClient();

            if (string.IsNullOrEmpty(raidLog.WarcraftLogsCode))
                raidLog.WarcraftLogsCode = raidLog.Url.PathAndQuery.Split("/").LastOrDefault();
            var logs = warcraftLogsClient.GetReport(raidLog.WarcraftLogsCode);
            var report = logs.Result.Value.ReportData.Report;
            if (report == null) return;
            
            raidLog.Name = report.Title;
            raidLog.Date = DateTimeOffset.FromUnixTimeMilliseconds((long)report.StartTime).UtcDateTime;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public override void UpdateCharacter(Character character, bool saveChanges)
    {
      
    }

    protected override void UpdateGuild(Guild guild)
    {
    
    }
}