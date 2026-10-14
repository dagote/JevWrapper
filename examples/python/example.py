#!/usr/bin/env python3
"""Example usage of the JevWrapper client in Python."""

from jevwrapper import JevClient, JevQuestions


def main():
    """Example Jev usage."""
    # Create a client
    client = JevClient()
    client.settings.api_key = "your-api-key-here"
    
    # Define questions
    questions = (
        JevQuestions()
        .add_choice(
            "action",
            "What should the enemy do?",
            "Attack",
            "Defend",
            "Retreat"
        )
        .describe("Attack", "Aggressive move toward the player")
        .describe("Defend", "Prepare defensive stance")
        .describe("Retreat", "Move away from engagement")
        .add_score(
            "confidence",
            "How confident is the enemy?",
            "Low",
            "Medium",
            "High"
        )
    )
    
    def handle_response(answer):
        """Handle the response from Jev."""
        if not answer.success:
            print(f"❌ Request failed: {answer.error}")
            print(f"Raw: {answer.raw_json}")
            return
        
        print(f"✓ Response for group: {answer.group}")
        for item in answer.items:
            print(f"  {item.name}:")
            print(f"    Type: {item.type.value}")
            if item.choice:
                print(f"    Choice: {item.choice}")
            if item.score is not None:
                print(f"    Score: {item.score}")
            if item.noul is not None:
                print(f"    Noul: {item.noul}")
            print(f"    Confidence: {item.confidence:.4f}")
            if item.probabilities:
                print(f"    Probabilities: {item.probabilities}")
    
    # Method 1: Use prose state description
    print("Example 1: With manual state description")
    client.question(
        group="enemy_01",
        questions=questions,
        on_answer=handle_response,
        state_text="Enemy health: 50/100. Enemy has 2 allies nearby.",
    )
    
    print("\nExample 2: With signals (auto-rendered state)")
    # Update some signals
    client.update("health", "enemy_02", 75.0)
    client.update("allies", "enemy_02", 1.0)
    client.update("threat_level", "enemy_02", 8.5)
    
    # Let the client render signals as state text
    client.question(
        group="enemy_02",
        questions=questions,
        on_answer=handle_response,
    )


if __name__ == "__main__":
    main()
