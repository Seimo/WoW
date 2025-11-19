namespace WarcraftLogsClient.Response;

public class Report
{

    public double StartTime { get; set; }
    public string Title { get; set; }
    public List<Fight> Fights { get; set; }
    public Guild Guild { get; set; }
    public User Owner { get; set; }
}