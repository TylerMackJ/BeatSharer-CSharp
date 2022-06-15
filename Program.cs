using System.Net.Http.Json;
using System.IO.Compression;
using dotenv.net;
using Microsoft.Extensions.Configuration;

public class BeatSharer
{
    static readonly HttpClient client = new HttpClient();

    static List<string> songIDList = new List<string>();
    static List<SongInfo> songInfoList = new List<SongInfo>();

    static List<Task> taskList = new List<Task>();

    static string CustomLevelsPath = Path.GetFullPath(".");

    public static async Task Main()
    {
        // Load Secrets
        IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<BeatSharer>().Build();

        // Get songIDList
        try
        {
            Console.Write("Enter an ID (0-255): ");
            string? userInput = Console.ReadLine();
            string address = String.Format("https://beat-sharer-default-rtdb.firebaseio.com/{0}.json?auth={1}", userInput, config["BeatSharer:APIKey"]);
            string songIDs = await client.GetStringAsync(address);
            if (songIDs == "null")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ID not found");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            songIDList = songIDs.Trim('"').Split(',').ToList();
        }
        catch (HttpRequestException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ID request failed");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        while (true)
        {
            // Purge existing songs
            List<string> foundIDs = new List<string>();
            string[] foundFolders = Directory.GetDirectories(CustomLevelsPath, "*", SearchOption.TopDirectoryOnly);
            foreach (string folder in foundFolders)
            {
                string foundSong = Path.GetFileName(folder);
                string[] splitTitle = foundSong.Split(" ");
                if (splitTitle.Length > 0)
                {
                    foundIDs.Add(splitTitle[0]);
                }
            }
            if (foundIDs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No songs found.");
                Console.WriteLine("Are you sure {0} is the correct folder?", CustomLevelsPath);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("(y/n): ");
                string? userInput = Console.ReadLine();
                if (userInput == null || userInput != "y")
                {
                    return;
                }
            }
            songIDList = songIDList.Except(foundIDs).ToList();

            // Get each song info
            taskList.Clear();
            songInfoList.Clear();
            foreach (string songID in songIDList)
            {
                if (songID != "")
                {
                    taskList.Add(GetSongInfo(songID));
                }
            }
            if (taskList.Count == 0)
            {
                break;
            }
            foreach (Task task in taskList)
            {
                await task;
            }
            if (songInfoList.Count == 0)
            {
                break;
            }

            // Download each song
            foreach (SongInfo songInfo in songInfoList)
            {
                taskList.Add(DownloadSong(songInfo));
            }
            foreach (Task task in taskList)
            {
                await task;
            }
        }
    }

    static Task GetSongInfo(string songID)
    {
        Task t = Task.Run(async () =>
            {
                await GetSongInfoTask(songID);  
            }
        );

        return t;
    }

    static async Task GetSongInfoTask(string songID)
    {
        try
        {
            Console.WriteLine("Getting song info for {0}", songID);
            string address = String.Format("https://api.beatsaver.com/maps/id/{0}", songID);
            SongInfo? songInfo = await client.GetFromJsonAsync<SongInfo>(address);
            if (songInfo != null)
            {
                songInfoList.Add(songInfo);
                Console.WriteLine("Found song info for {0}", songID);
            }
        }
        catch (HttpRequestException)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Could not find song info for {0}", songID);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

    static Task DownloadSong(SongInfo songInfo)
    {
        Task t = Task.Run(async () =>
            {
                await DownloadSongTask(songInfo);
            }
        );

        return t;
    }

    static async Task DownloadSongTask(SongInfo songInfo)
    {
        try
        {
            if (songInfo.versions != null && songInfo.versions.Length > 0 && songInfo.versions[0].downloadURL != null)
            {
                Console.WriteLine("Downloading {0}", songInfo.GetSongTitle());
                byte[] bytes = await client.GetByteArrayAsync(songInfo.versions[0].downloadURL);
                MemoryStream compressedStream = new MemoryStream(bytes);
                ZipArchive zipStream = new ZipArchive(compressedStream);
                MemoryStream decompressedStream = new MemoryStream();
                zipStream.ExtractToDirectory(Path.Combine(CustomLevelsPath, songInfo.GetSongTitle()));
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Downloaded {0}", songInfo.GetSongTitle());
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        catch (HttpRequestException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed download for {0}", songInfo.GetSongTitle());
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}