using System;
using System.Threading.Tasks;
using JevWrapper;

class Program
{
    static async Task Main()
    {
        // Create a client with your settings
        var client = new JevClient();
        client.Settings.ApiKey = "your-api-key-here";
        
        // Create a question
        var questions = new JevQuestions()
            .AddChoice(
                "action",
                "What should the enemy do?",
                "Attack",
                "Defend",
                "Retreat"
            )
            .Describe("Attack", "Aggressive move toward the player")
            .Describe("Defend", "Prepare defensive stance")
            .Describe("Retreat", "Move away from engagement")
            .AddScore(
                "confidence",
                "How confident is the enemy?",
                "Low",
                "Medium",
                "High"
            );
        
        try
        {
            // Example 1: With manual state description
            Console.WriteLine("Example 1: With manual state description");
            var answer1 = await client.QuestionAsync(
                group: "enemy_01",
                questions: questions,
                stateText: "Enemy health: 50/100. Enemy has 2 allies nearby."
            );
            
            PrintAnswer(answer1);
            
            // Example 2: With signals (auto-rendered state)
            Console.WriteLine("\nExample 2: With signals (auto-rendered state)");
            client.Update("health", "enemy_02", 75.0f);
            client.Update("allies", "enemy_02", 1.0f);
            client.Update("threat_level", "enemy_02", 8.5f);
            
            var answer2 = await client.QuestionAsync(
                group: "enemy_02",
                questions: questions
            );
            
            PrintAnswer(answer2);
        }
        finally
        {
            client.Dispose();
        }
    }
    
    static void PrintAnswer(JevAnswer answer)
    {
        if (!answer.Success)
        {
            Console.WriteLine($"❌ Request failed: {answer.Error}");
            if (!string.IsNullOrEmpty(answer.RawJson))
                Console.WriteLine($"Raw: {answer.RawJson}");
            return;
        }
        
        Console.WriteLine($"✓ Response for group: {answer.Group}");
        foreach (var item in answer.Items)
        {
            Console.WriteLine($"  {item.Name}:");
            Console.WriteLine($"    Type: {item.Type}");
            if (item.Choice != null)
                Console.WriteLine($"    Choice: {item.Choice}");
            if (item.Score.HasValue)
                Console.WriteLine($"    Score: {item.Score}");
            if (item.Noul.HasValue)
                Console.WriteLine($"    Noul: {item.Noul}");
            Console.WriteLine($"    Confidence: {item.Confidence:F4}");
            if (item.Probabilities?.Count > 0)
                Console.WriteLine($"    Probabilities: {string.Join(", ", item.Probabilities)}");
        }
    }
}
