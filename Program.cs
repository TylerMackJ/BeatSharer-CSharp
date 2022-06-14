using System.Net.Http.Json;
using System.IO.Compression;
using dotenv.net;

public class BeatSharer
{
    static readonly HttpClient client = new HttpClient();

    static List<string> songIDList = new List<string>();
    static List<SongInfo> songInfoList = new List<SongInfo>();

    static List<Task> taskList = new List<Task>();

    static string CustomLevelsPath = Path.Combine(Path.GetFullPath("."), "Downloads");

    public static async Task Main()
    {
        // Load secret
        DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: false));

        // Get songIDList
        string listIndex = "2";
        string address = String.Format("https://beat-sharer-default-rtdb.firebaseio.com/{0}.json?auth={1}", listIndex, DotEnv.Read()["SECRET"]);
        string songIDs = await client.GetStringAsync(address);
        songIDList = songIDs.Trim('"').Split(',').ToList();

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
            Console.WriteLine("No songs found. Be sure you have selected the rigth folder");
            return;
        }
        songIDList = songIDList.Except(foundIDs).ToList();

        // Get each song info
        foreach (string songID in songIDList)
        {
            if (songID != "")
            {
                taskList.Add(GetSongInfo(songID));
            }
        }
        foreach (Task task in taskList)
        {
            task.Wait();
        }

        // Download each song
        foreach (SongInfo songInfo in songInfoList)
        {
            taskList.Add(DownloadSong(songInfo));
        }
        foreach (Task task in taskList)
        {
            task.Wait();
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
        Console.WriteLine("Getting song info for {0}", songID);
        string address = String.Format("https://api.beatsaver.com/maps/id/{0}", songID);
        SongInfo? songInfo = await client.GetFromJsonAsync<SongInfo>(address);
        if (songInfo != null)
        {
            songInfoList.Add(songInfo);
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
        if (songInfo.versions != null && songInfo.versions.Length > 0 && songInfo.versions[0].downloadURL != null)
        {
            Console.WriteLine("Downloading {0}", songInfo.GetSongTitle());
            byte[] bytes = await client.GetByteArrayAsync(songInfo.versions[0].downloadURL);
            MemoryStream compressedStream = new MemoryStream(bytes);
            ZipArchive zipStream = new ZipArchive(compressedStream);
            MemoryStream decompressedStream = new MemoryStream();
            zipStream.ExtractToDirectory(Path.Combine(CustomLevelsPath, songInfo.GetSongTitle()));
        }
    }
}