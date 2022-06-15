using System.Net.Http.Json;

public class Upload
{
    private static int currentCode = 0;
    public static async Task Run()
    {
        // Get song list
        List<string> foundIDs = Shared.FindExistingSongs(BeatSharer.CustomLevelsPath);
        if (foundIDs.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No songs found in {0}", BeatSharer.CustomLevelsPath);
            Console.ForegroundColor = ConsoleColor.Gray;
            return;
        }
        string uploadString = string.Join(',', foundIDs);
        Console.WriteLine("Found {0} songs", foundIDs.Count);

        // Get current code
        try
        {
            Console.WriteLine("Getting song list code");
            string codeAddress = String.Format("https://beat-sharer-default-rtdb.firebaseio.com/index.json?auth={0}", BeatSharer.config["BeatSharer:APIKey"]);
            string response = (await BeatSharer.client.GetStringAsync(codeAddress)).Trim('"'); ;

            try
            {
                currentCode = int.Parse(response);
            }
            catch (FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Bad code '{0}' found. Defaulting to 0", response);
                Console.WriteLine("Please report this occurance.");
                Console.ForegroundColor = ConsoleColor.Gray;
                currentCode = 0;
            }
        }
        catch (HttpRequestException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Code request failed");
            Console.ForegroundColor = ConsoleColor.Gray;
            return;
        }

        // Upload song list
        try
        {
            Console.WriteLine("Uploading song list");
            string uploadAddress = String.Format("https://beat-sharer-default-rtdb.firebaseio.com/{0}.json?auth={1}", currentCode.ToString(), BeatSharer.config["BeatSharer:APIKey"]);
            await BeatSharer.client.PutAsync(uploadAddress, JsonContent.Create(uploadString));
            // Update Index
            int nextCode = currentCode + 1;
            if (nextCode >= 256)
            {
                nextCode = 0;
            }
            uploadAddress = String.Format("https://beat-sharer-default-rtdb.firebaseio.com/index.json?auth={0}", BeatSharer.config["BeatSharer:APIKey"]);
            await BeatSharer.client.PutAsync(uploadAddress, JsonContent.Create(nextCode));
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\nUploaded song list to {0} (Use this code to share your song list)", currentCode);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        catch (HttpRequestException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Upload request failed");
            Console.ForegroundColor = ConsoleColor.Gray;
            return;
        }
    }
}