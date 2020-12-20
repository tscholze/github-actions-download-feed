using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text;

namespace ConsoleApp1
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Conversion started.");
            FeedsToJson();
        }

        private static async void FeedsToJson()
        {
            // List of found items.
            var foundItems = new List<SyndicationItem>();

            // Get all files from input folder.
            var filePaths = Directory.GetFiles(@"/Users/tobiasscholze/Desktop/input", "*.rss");

            // Parse each file and add result to found items.
            foreach (var path in filePaths)
            {
                var reader = XmlReader.Create(path);
                var formatter = new Rss20FeedFormatter();
                formatter.ReadFrom(reader);
                reader.Close();
                foundItems.AddRange(formatter.Feed.Items);
            }

            // Make a distinct list of found items.
            // and transfer items to new data object structure.
            var items = foundItems
                .GroupBy(i => i.Id)
                .Select(g => g.First())
                .ToList()
                .Select(i => new Post(item: i))
                .ToList();

            // 1. Try
            // Serialize distinct items to JSON.
            // and write json file to disk.
            using FileStream createStream = File.Create("/Users/tobiasscholze/Desktop/result.json"); await JsonSerializer.SerializeAsync(createStream, items);

            // 2. Try
            //await File.WriteAllBytesAsync("/Users/tobiasscholze/Desktop/result.json", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items)));

            // 3. Try
            //var foo = JsonSerializer.SerializeToUtf8Bytes(items);
            //using (StreamWriter writer = new(File.Open("/Users/tobiasscholze/Desktop/result.json", FileMode.Create), Encoding.UTF8))
            //{
            //    await writer.WriteAsync(Encoding.UTF8.GetString(foo));
            //}
        }
    }
}

/// <summary>
/// Model of a simplified post.
/// </summary>
internal class Post
{
    #region Members
    
    /// <summary>
    /// Id of the post.
    /// Renders as `id` to JSON.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; }
    
    /// <summary>
    /// Title of the post.
    /// Renders as `tile` to JSON.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; }
    
    /// <summary>
    /// Article image url of the post.
    /// Renders as `article_image_url` to JSON.
    /// </summary>
    [JsonPropertyName("article_image_url")]
    public string ArticleImageUrl { get; }
    
    /// <summary>
    /// Plain text summary of the post.
    /// Renders as `summary` to JSON.
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; }
    
    /// <summary>
    /// Published date of the post.
    /// Renders as `pub_date` to JSON.
    /// </summary>
    [JsonPropertyName("pub_date")]
    public DateTimeOffset PupDate { get; }
    
    #endregion
    
    #region Private constants

    /// <summary>
    /// Fallback for the image source of a post.
    /// </summary>
    private const string ImagesourceFallback = "https://www.drwindows.de/news/wp-content/themes/drwindows_theme/img/DrWindows-Windows-News.png";
    
    #endregion
    
    #region Constructor

    /// <summary>
    /// Post constructor to create a new instance from
    /// a given SyndicationItem.
    /// </summary>
    /// <param name="item">SyndicationItem input.</param>
    public Post(SyndicationItem item)
    {
        Id = item.Id;
        Title = item.Title.Text;
        Summary = GetTextFromHtml(item.Summary.Text);
        ArticleImageUrl = GetImageSourceOutOfContent(item.Summary.Text);
        PupDate = item.PublishDate;
    }
    
    #endregion
    
    #region Private helper

    /// <summary>
    /// Gets plain text from html string.
    /// </summary>
    /// <param name="html">Html input string.</param>
    /// <returns>Pure text from html.</returns>
    private static string GetTextFromHtml(string html)
    {
        return Regex.Replace(html, "<.*?>", string.Empty);
    }

    /// <summary>
    /// Gets the image source out of a content string.
    /// </summary>
    /// <param name="content">Content string of a post.</param>
    /// <returns>Found or fallback image source.</returns>
    private static string GetImageSourceOutOfContent(string content)
    {
        // Try to extract the first `<img src=".." /> out of the content string 
        // to use as image source.
        var source = Regex.Match(content, "<img.+?src=[\"'](.+?)[\"'].*?>", RegexOptions.IgnoreCase)
            .Groups[1]
            .Value;

        // If no image found in content, use fallback.
        return source.Length != 0 ? source : ImagesourceFallback;
    }

    #endregion
}