using System.Net.Http.Json;
using System.IO.Compression;

public class Download
{
    private static List<string> songIDList = new List<string>();
    private static List<SongInfo> songInfoList = new List<SongInfo>();

    private static List<Task> taskList = new List<Task>();

    private static List<SongInfo> DownloadedList = new List<SongInfo>();
    private static List<string> NoInfoList = new List<String>();

    public static async Task Run()
    {
        // Get songIDList
        try
        {
            Console.Write("Enter an song list code (0-255): ");
            string? userInput = Console.ReadLine();
            string address = String.Format("https://beat-sharer-default-rtdb.firebaseio.com/{0}.json?auth={1}", userInput, BeatSharer.config["BeatSharer:APIKey"]);
            string songIDs = await BeatSharer.client.GetStringAsync(address);
            if (songIDs == "null")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Code not found");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            songIDList = songIDs.Trim('"').Split(',').ToList();
        }
        catch (HttpRequestException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Code request failed");
            Console.WriteLine(e);
            Console.ForegroundColor = ConsoleColor.Gray;
            return;
        }

        while (true)
        {
            // Purge existing songs
            List<string> foundIDs = Shared.FindExistingSongs(BeatSharer.CustomLevelsPath);
            if (foundIDs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No songs found.");
                Console.WriteLine("Is {0} is the correct folder?", BeatSharer.CustomLevelsPath);
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
            NoInfoList.Clear();
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
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        if (DownloadedList.Count > 0)
        {
            Console.WriteLine("\nDownloaded:");
            foreach (SongInfo songInfo in DownloadedList)
            {
                Console.WriteLine("\t{0}", songInfo.GetSongTitle());
            }
        }
        else
        {
            Console.WriteLine("\nNo songs missing");
        }

        if (NoInfoList.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nCould not find info:");
            foreach (string songID in NoInfoList)
            {
                Console.WriteLine("\t{0}", songID);
            }
        }
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    private static Task GetSongInfo(string songID)
    {
        Task t = Task.Run(async () =>
            {
                await GetSongInfoTask(songID);
            }
        );

        return t;
    }

    private static async Task GetSongInfoTask(string songID)
    {
        try
        {
            Console.WriteLine("Getting song info for {0}", songID);
            string address = String.Format("https://api.beatsaver.com/maps/id/{0}", songID);
            HttpResponseMessage resp = await BeatSharer.client.GetAsync(address);

            if ((int)resp.StatusCode == 429)
            {
                Console.WriteLine("Rate limit reached. Retrying");
                await GetRatelimit(resp.Headers).Wait();
                return;
            }
            else if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                SongInfo? songInfo = System.Text.Json.JsonSerializer.Deserialize<SongInfo>(await resp.Content.ReadAsStringAsync());

                if (songInfo != null)
                {
                    songInfoList.Add(songInfo);
                    Console.WriteLine("Found song info for {0}", songID);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Could not find song info for {0}", songID);
                Console.ForegroundColor = ConsoleColor.Gray;
                NoInfoList.Add(songID);
            }
        }
        catch (HttpRequestException e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Could not find song info for {0}", songID);
            Console.WriteLine(e);
            Console.ForegroundColor = ConsoleColor.Gray;
            NoInfoList.Add(songID);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static Ratelimit GetRatelimit(System.Net.Http.Headers.HttpResponseHeaders headers)
    {
        Ratelimit ratelimit = new Ratelimit();

        if (headers.TryGetValues("Rate-Limit-Remaining", out IEnumerable<string>? _remaining))
        {
            var Remaining = _remaining.GetEnumerator();
            Remaining.MoveNext();
            ratelimit.Remaining = int.Parse(Remaining.Current);
            Remaining.Dispose();
        }

        if (headers.TryGetValues("Rate-Limit-Reset", out IEnumerable<string>? _reset))
        {
            var Reset = _reset.GetEnumerator();
            Reset.MoveNext();
            ratelimit.Reset = int.Parse(Reset.Current);
            ratelimit.ResetTime = UnixTimestampToDateTime((long)ratelimit.Reset);
            Reset.Dispose();
        }

        if (headers.TryGetValues("Rate-Limit-Total", out IEnumerable<string>? _total))
        {
            var Total = _total.GetEnumerator();
            Total.MoveNext();
            ratelimit.Total = int.Parse(Total.Current);
            Total.Dispose();
        }

        return ratelimit;
    }

    private static DateTime UnixTimestampToDateTime(double unixTime)
    {
        DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
        return new DateTime(unixStart.Ticks + unixTimeStampInTicks, DateTimeKind.Utc);
    }



    private static Task DownloadSong(SongInfo songInfo)
    {
        Task t = Task.Run(async () =>
            {
                await DownloadSongTask(songInfo);
            }
        );

        return t;
    }

    private static async Task DownloadSongTask(SongInfo songInfo)
    {
        try
        {
            if (songInfo.versions != null && songInfo.versions.Length > 0 && songInfo.versions[0].downloadURL != null)
            {
                Console.WriteLine("Downloading {0}", songInfo.GetSongTitle());
                byte[] bytes = await BeatSharer.client.GetByteArrayAsync(songInfo.versions[0].downloadURL);
                MemoryStream compressedStream = new MemoryStream(bytes);
                ZipArchive zipStream = new ZipArchive(compressedStream);
                MemoryStream decompressedStream = new MemoryStream();
                zipStream.ExtractToDirectory(Path.Combine(BeatSharer.CustomLevelsPath, songInfo.GetSongTitle()));
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Downloaded {0}", songInfo.GetSongTitle());
                Console.ForegroundColor = ConsoleColor.Gray;
                DownloadedList.Add(songInfo);
            }
        }
        catch (HttpRequestException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed download for {0}", songInfo.GetSongTitle());
            Console.WriteLine(e);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

    private class Ratelimit
    {
        public int? Remaining { get; set; }
        public int? Total { get; set; }
        public int? Reset { get; set; }
        public DateTime ResetTime { get; set; }
        public async Task Wait()
        {
            await Task.Delay(new TimeSpan(Math.Max(ResetTime.Ticks - DateTime.UtcNow.Ticks, 0)));
        }
    }
}

