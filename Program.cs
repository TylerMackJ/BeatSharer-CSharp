using Microsoft.Extensions.Configuration;

public class BeatSharer
{
    public static IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<BeatSharer>().Build();

    public static readonly HttpClient client = new HttpClient();

    public static string CustomLevelsPath = Path.GetFullPath(".");

    public static async Task Main()
    {
        Console.Write("Would you like to upload your current song list (y/n): ");
        string? userInput = Console.ReadLine();
        if (userInput != null && userInput == "y")
        {
            await Upload.Run();
        }
        Console.Write("Would you like to download a song list (y/n): ");
        userInput = Console.ReadLine();
        if (userInput != null && userInput == "y")
        {
            await Download.Run();
        }
    }
}