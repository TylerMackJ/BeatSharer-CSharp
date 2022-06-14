class SongInfo
{
    public string? id { get; set; } = null;
    public string? name { get; set; } = null;
    public string? description { get; set; } = null;
    public string? downloadURL { get; set; } = null;
    public Uploader? uploader { get; set; } = null;
    public Metadata? metadata { get; set; } = null;
    public Stats? stats { get; set; } = null;
    public DateTimeOffset? uploaded { get; set; } = null;
    public bool? automapper { get; set; } = null;
    public bool? ranked { get; set; } = null;
    public bool? qualified { get; set; } = null;
    public Version[]? versions { get; set; } = null;
    public DateTimeOffset? createdAt { get; set; } = null;
    public DateTimeOffset? updatedAt { get; set; } = null;
    public DateTimeOffset? lastPublishedAt { get; set; } = null;

    public class Uploader
    {
        public int? id { get; set; } = null;
        public string? name { get; set; } = null;
        public string? hash { get; set; } = null;
        public string? avatar { get; set; } = null;
        public string? type { get; set; } = null;
        public bool? curator { get; set; } = null;
        public bool? verifiedMapper { get; set; } = null;
    }
    public class Metadata
    {
        public float? bpm { get; set; } = null;
        public int? duration { get; set; } = null;
        public string? songName { get; set; } = null;
        public string? songSubName { get; set; } = null;
        public string? songAuthorName { get; set; } = null;
        public string? levelAuthorName { get; set; } = null;
    }
    public class Stats
    {
        public int? plays { get; set; } = null;
        public int? downloads { get; set; } = null;
        public int? upvotes { get; set; } = null;
        public int? downvotes { get; set; } = null;
        public float? score { get; set; } = null;
    }
    public class Version
    {
        public string? hash { get; set; } = null;
        public string? key { get; set; } = null;
        public string? state { get; set; } = null;
        public DateTimeOffset? createdAt { get; set; } = null;
        public int? sageScore { get; set; } = null;
        public Diff[]? diffs { get; set; } = null;
        public string? downloadURL { get; set; } = null;
        public string? coverURL { get; set; } = null;
        public string? previewURL { get; set; } = null;

        public class Diff
        {
            public float? njs { get; set; } = null;
            public float? offset { get; set; } = null;
            public int? notes { get; set; } = null;
            public int? bombs { get; set; } = null;
            public int? obstacles { get; set; } = null;
            public float? nps { get; set; } = null;
            public float? length { get; set; } = null;
            public string? characteristic { get; set; } = null;
            public string? difficulty { get; set; } = null;
            public int? events { get; set; } = null;
            public bool? chroma { get; set; } = null;
            public bool? me { get; set; } = null;
            public bool? ne { get; set; } = null;
            public bool? cinema { get; set; } = null;
            public float? seconds { get; set; } = null;
            public ParitySummary? paritySummary { get; set; } = null;
            public float? stars { get; set; } = null;
            public int? maxScore { get; set; } = null;

            public class ParitySummary
            {
                public int? errors { get; set; } = null;
                public int? warns { get; set; } = null;
                public int? resets { get; set; } = null;
            }
        }
    }

    public string GetSongTitle()
    {
        string? songName = "";
        string? levelAuthorName = "";
        if (this.metadata != null)
        {
            songName = this.metadata.songName;
            levelAuthorName = this.metadata.levelAuthorName;
        }
        return String.Format("{0} ({1} - {2})", this.id, songName, levelAuthorName);
    }
} 
