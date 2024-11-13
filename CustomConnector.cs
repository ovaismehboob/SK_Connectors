using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.SemanticKernel.Plugins.Web;

public class HuggingFaceConnector : IWebSearchEngineConnector
{
    private readonly HttpClient _httpClient;

    public HuggingFaceConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> SearchAsync(string query)
    {
        // Step 1: Access Model Metadata
        var response = await _httpClient.GetStringAsync("https://huggingface.co/api/models");
        var models = JsonSerializer.Deserialize<List<ModelMetadata>>(response);

        if (models == null)
        {
            return "Failed to retrieve models.";
        }

        // Step 2: Search for Specific Model
        var model = models.FirstOrDefault(m => m.id != null && m.id.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (model == null)
        {
            return $"Model with id containing '{query}' not found.";
        }

        // Step 3: Scrape Model Information
        var githubUrl = $"https://github.com/{model.id}";
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(githubUrl);

        var modelInfo = new ModelInfo
        {
            modelName = model.id,
            modelDescription = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", ""),
            history = doc.DocumentNode.SelectSingleNode("//div[@id='history']")?.InnerText,
            features = doc.DocumentNode.SelectSingleNode("//div[@id='features']")?.InnerText,
            yearReleased = doc.DocumentNode.SelectSingleNode("//div[@id='year-released']")?.InnerText,
            videoLink = doc.DocumentNode.SelectSingleNode("//a[@id='video-link']")?.GetAttributeValue("href", ""),
            newReleases = doc.DocumentNode.SelectSingleNode("//div[@id='new-releases']")?.InnerText
        };

        // Step 4: Output Format
        return JsonSerializer.Serialize(modelInfo);
    }

    public Task<IEnumerable<string>> SearchAsync(string query, int count = 1, int offset = 0, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

}

public class ModelMetadata
{
    public string _id { get; set; }
    public string id { get; set; }
    public int likes { get; set; }
    public int downloads { get; set; }
    public List<string> tags { get; set; }
    public string pipelineTag { get; set; }
    public string libraryName { get; set; }
    public DateTime createdAt { get; set; }
    public string modelId { get; set; }
    // Other properties...
}

public class ModelInfo
{
    public string modelName { get; set; }
    public string modelDescription { get; set; }
    public string history { get; set; }
    public string features { get; set; }
    public string yearReleased { get; set; }
    public string videoLink { get; set; }
    public string newReleases { get; set; }
}
