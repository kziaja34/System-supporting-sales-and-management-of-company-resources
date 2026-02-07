using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using UglyToad.PdfPig;

namespace SSSMCR.ApiService.Services.AI;

#pragma warning disable SKEXP0001 
#pragma warning disable SKEXP0070

public class RagService
{
    private ISemanticTextMemory _memory;
    private const string CollectionName = "ThesisMemory";
    private bool _isInitialized = false;

    public RagService(IConfiguration config)
    {
    }
    
    [Experimental("SKEXP0050")]
    public async Task InitializeAsync(string pdfPath)
    {
        if (_isInitialized) return;
        
        try 
        {
            var ollamaService = new SimpleOllamaEmbeddingService(
                endpoint: "http://localhost:11434",
                modelId: "nomic-embed-text"
            );

            var memoryBuilder = new MemoryBuilder();
            memoryBuilder.WithTextEmbeddingGeneration(ollamaService);
            memoryBuilder.WithMemoryStore(new VolatileMemoryStore());
            
            _memory = memoryBuilder.Build();
            
            await IngestPdfAsync(pdfPath);
            
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n!!! RAG ERROR (Ollama) !!!");
            Console.WriteLine($"Szczegóły: {ex.Message}");
            _memory = null; 
        }
    }

    private async Task IngestPdfAsync(string pdfPath)
    {
        if (!File.Exists(pdfPath))
        {
            Console.WriteLine($"--> RAG Error: Nie znaleziono pliku: {pdfPath}");
            return;
        }

        Console.WriteLine($"--> RAG (Ollama): Indeksowanie dokumentacji: {Path.GetFileName(pdfPath)}...");
        
        using var document = PdfDocument.Open(pdfPath);
        int counter = 0;

        foreach (var page in document.GetPages())
        {
            var rawText = page.Text;
            
            // UŻYWAMY NOWEJ METODY DO DZIELENIA TEKSTU NA MNIEJSZE KAWAŁKI
            // Limit 1500 znaków jest bezpieczny dla modelu nomic-embed-text
            var chunks = GetSmartChunks(rawText, 1500);

            foreach (var chunk in chunks)
            {
                await _memory.SaveInformationAsync(
                    collection: CollectionName,
                    id: $"Page{page.Number}_Para{counter}",
                    text: chunk,
                    description: $"Strona {page.Number}"
                );

                counter++;
            }
        }
        Console.WriteLine($"--> RAG: Gotowe. Zapisano {counter} fragmentów wiedzy w pamięci.");
    }

    // --- NOWA METODA: INTELIGENTNE DZIELENIE TEKSTU ---
    private List<string> GetSmartChunks(string text, int maxChunkSize)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        // 1. Czyścimy tekst z dziwnych znaków nowej linii, które PDF robi w środku zdań
        var cleanText = text.Replace("\r", " ").Replace("\n", " ");
        
        // 2. Dzielimy na zdania (zgrubnie)
        var sentences = cleanText.Split(new[] { ". ", "? ", "! " }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new StringBuilder();

        foreach (var sentence in sentences)
        {
            // Dodajemy kropkę, którą Split usunął
            var sentenceWithDot = sentence.Trim() + ". ";

            // Jeśli dodanie zdania przekroczy limit, zapisujemy obecny chunk i zaczynamy nowy
            if (currentChunk.Length + sentenceWithDot.Length > maxChunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                // Zabezpieczenie: Jeśli samo pojedyncze zdanie jest gigantyczne (np. tabela wklejona jako tekst)
                // Musimy je pociąć na sztywno
                if (sentenceWithDot.Length > maxChunkSize)
                {
                    var subChunks = Enumerable.Range(0, (sentenceWithDot.Length + maxChunkSize - 1) / maxChunkSize)
                                      .Select(i => sentenceWithDot.Substring(i * maxChunkSize, Math.Min(maxChunkSize, sentenceWithDot.Length - i * maxChunkSize)));
                    chunks.AddRange(subChunks);
                }
                else
                {
                    currentChunk.Append(sentenceWithDot);
                }
            }
            else
            {
                currentChunk.Append(sentenceWithDot);
            }
        }

        // Dodajemy ostatni kawałek
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
    
    public async Task<string> SearchAsync(string query)
    {
        if (!_isInitialized || _memory == null) return "";
        
        try 
        {
            var results = _memory.SearchAsync(CollectionName, query, limit: 3, minRelevanceScore: 0.5);
            
            List<string> snippets = new();
            await foreach (var result in results)
            {
                snippets.Add($"[Dokumentacja - Strona {result.Metadata.Description}]:\n{result.Metadata.Text}");
            }

            if (snippets.Count == 0) return "";

            return string.Join("\n\n---\n\n", snippets);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd wyszukiwania: {ex.Message}");
            return "";
        }
    }
}

// ==========================================
// ADAPTER OLLAMA (Bez zmian)
// ==========================================
public class SimpleOllamaEmbeddingService : ITextEmbeddingGenerationService
{
    private readonly string _endpoint;
    private readonly string _modelId;
    private readonly HttpClient _httpClient;

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    public SimpleOllamaEmbeddingService(string endpoint, string modelId)
    {
        _endpoint = endpoint.TrimEnd('/');
        _modelId = modelId;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Dłuższy timeout dla bezpieczeństwa
    }

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        var result = new List<ReadOnlyMemory<float>>();

        foreach (var text in data)
        {
            var cleanText = text.Replace("\0", "").Trim();
            if (string.IsNullOrWhiteSpace(cleanText)) continue;

            var requestBody = new
            {
                model = _modelId,
                prompt = cleanText
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            try 
            {
                var response = await _httpClient.PostAsync($"{_endpoint}/api/embeddings", content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken);
                    Console.WriteLine($"\n[OLLAMA HTTP {response.StatusCode}]: {errorDetails}");
                    throw new HttpRequestException($"Ollama Error: {errorDetails}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(jsonResponse);

                if (embeddingResponse?.Embedding != null)
                {
                    float[] floatArray = Array.ConvertAll(embeddingResponse.Embedding, item => (float)item);
                    result.Add(new ReadOnlyMemory<float>(floatArray));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd Ollama: {ex.Message}");
                throw;
            }
        }

        return result;
    }

    private class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public double[]? Embedding { get; set; }
    }
}