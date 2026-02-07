using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OpenAI;
using OpenAI.Chat;
using SSSMCR.ApiService.Services.AI;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public interface IAiAssistantService
{
    Task<string> GetAnswerAsync(ChatRequest request);
}

[Experimental("SKEXP0001")]
public class AiAssistantService : IAiAssistantService
{
    private readonly ChatClient _chatClient;
    private readonly RagService _ragService;
    private readonly string _modelName;
    private readonly Dictionary<string, string> _rolePrompts = new()
    {
        { 
            "general", 
            """
            You are a Virtual Assistant in the 'System Supporting Sales and Management of Company Resources' (SSSMCR).
            Your goal is to help users navigate the ERP system and understand key metrics.

            Key System Knowledge:
            1. UNIQUE FEATURE: The system uses a FUZZY LOGIC algorithm to calculate Order Priorities dynamically. It is not just simple sorting by date.
            2. Modules: Warehouse, Sales, Invoices, Administration (Users/Branches).
            3. Tech Stack: The system is built on ASP.NET Core 9 and Blazor.

            Respond concisely, professionally, and strictly in English.
            If you don't know the answer, suggest contacting the system administrator.
            """ 
        },
        { 
            "sales", 
            """
            You act as a Sales Support Assistant. You help sales representatives process client orders.

            Domain Knowledge for this Module:
            1. PRIORITY CALCULATION (Crucial): Order priority is determined by the 'FuzzyPriorityEvaluatorService'. 
               It analyzes inputs like: Order Value, Number of Items, and Deadline urgency. 
               Explain this if a user asks "Why is this order priority High/Low?".
            2. RESERVATIONS: Creating an Order creates a 'StockReservation'. It does not immediately decrease physical 'ProductStock' until the order is finalized.
            3. CLIENTS: Check if the client has an active credit limit before suggesting large orders.

            Tone: Helpful and sales-oriented.
            """ 
        },
        { 
            "warehouse", 
            """
            You act as the Warehouse Manager. Your priority is inventory accuracy and logistics.

            Warehouse Rules:
            1. STOCK LEVELS: 'ProductStock' represents physical items on the shelf.
            2. AVAILABLE STOCK: Calculate as (Physical Stock - Reservations). Always warn if Available Stock is negative.
            3. MOVEMENTS:
               - Supply Arrival -> Increases Stock.
               - Order Fulfillment -> Decreases Stock (releases Reservation).
               - StockMovement -> Moves items between warehouse sectors/shelves.
            4. SHORTAGES: If items are missing, suggest creating a 'SupplyOrder' from a Supplier.

            Use professional logistics terminology.
            """ 
        },
        { 
            "invoices", 
            """
            You act as the Chief Accountant. You ensure financial document correctness.

            Financial Rules:
            1. INVOICING: An Invoice can typically only be generated for Orders that are fully processed/completed.
            2. TAXES: The system handles VAT rates defined per Product. Standard rate is usually 23%.
            3. TERMS: Payment deadlines are calculated based on Client terms.

            Be precise with numbers and terms like Net, Gross, VAT, and Due Date.
            """ 
        },
        { 
            "admin", 
            """
            You act as the System Administrator. You assist with user management and security.

            Technical & Security Knowledge:
            1. RBAC: Access is controlled via Roles (e.g., Admin, Manager, Sales, Warehouse).
            2. BRANCHES: Users and Stocks are assigned to specific Company Branches.
            3. SECURITY: Passwords are hashed. You cannot retrieve a lost password, only reset it.

            Always warn the user about the consequences of deleting data (e.g., removing a User or Branch).
            """ 
        }
    };
    
    public AiAssistantService(IConfiguration configuration, RagService ragService)
    {
        _ragService = ragService;
        
        var apiKey = configuration["Groq:ApiKey"];
        var endpoint = configuration["Groq:Endpoint"];
        _modelName = configuration["Groq:Model"] ?? "llama3-70b-8192";
        
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint!)
        };
        
        // Inicjalizacja klienta
        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey!), clientOptions);
        _chatClient = openAiClient.GetChatClient(_modelName);
    }

    public async Task<string> GetAnswerAsync(ChatRequest request)
    {
        // 1. Najpierw szukamy informacji w dokumentacji (RAG)
        string knowledge = await _ragService.SearchAsync(request.UserMessage);

        // 2. Wybieramy rolę
        string baseRole = _rolePrompts.ContainsKey(request.ContextKey) 
            ? _rolePrompts[request.ContextKey] 
            : _rolePrompts["general"];

        // 3. Budujemy System Prompt (Rola + Wiedza z PDF)
        var sb = new StringBuilder();
        sb.AppendLine(baseRole);
        
        if (!string.IsNullOrEmpty(knowledge))
        {
            sb.AppendLine("\n### KONTEKST Z DOKUMENTACJI TECHNICZNEJ (PRACA INŻYNIERSKA):");
            sb.AppendLine("Użyj poniższych informacji, aby precyzyjnie odpowiedzieć na pytanie użytkownika. Jeśli odpowiedź jest w tekście, zacytuj ją.");
            sb.AppendLine(knowledge);
        }
        else
        {
            sb.AppendLine("\n(Brak informacji w dokumentacji technicznej - korzystaj z wiedzy ogólnej).");
        }

        // 4. Wysyłamy do AI
        List<ChatMessage> messages = new()
        {
            new SystemChatMessage((string)sb.ToString()),
            new UserChatMessage(request.UserMessage)
        };

        try
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);
            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            return $"Błąd AI: {ex.Message}";
        }
    }
}