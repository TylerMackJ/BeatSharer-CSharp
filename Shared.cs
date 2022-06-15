public class Shared
{
    public static List<string> FindExistingSongs(string CustomLevelsPath)
    {
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

        return foundIDs;
    }
}