new Game().Run();

public class Game
{
    private const int MaxWrong = 6;

    private readonly Random _rng = new();
    private readonly HashSet<string> _used = new(); // words already played, to avoid repeats
    private int _wins;
    private int _losses;

    // Add your own words: lowercase letters only, no spaces or hyphens.
    private static readonly Dictionary<string, string[]> Categories = new()
    {
        ["Animals"] = new[]
        {
            "elephant", "giraffe", "kangaroo", "penguin", "dolphin", "crocodile",
            "butterfly", "octopus", "hedgehog", "flamingo", "squirrel", "chameleon",
            "cheetah", "porcupine", "seahorse", "platypus"
        },
        ["Countries"] = new[]
        {
            "philippines", "singapore", "australia", "argentina", "switzerland",
            "indonesia", "portugal", "thailand", "norway", "vietnam", "morocco",
            "malaysia", "colombia", "iceland", "germany", "jamaica"
        },
        ["Food"] = new[]
        {
            "spaghetti", "pineapple", "chocolate", "avocado", "dumpling", "pancake",
            "broccoli", "sandwich", "mushroom", "cinnamon", "strawberry", "burrito",
            "lasagna", "noodles", "mango", "coconut"
        },
        ["Programming"] = new[]
        {
            "variable", "function", "compiler", "debugger", "algorithm", "database",
            "recursion", "interface", "keyboard", "software", "developer", "inheritance",
            "framework", "exception", "iterator", "polymorphism"
        },
        ["Space"] = new[]
        {
            "asteroid", "galaxy", "telescope", "astronaut", "satellite", "meteor",
            "supernova", "universe", "gravity", "eclipse", "nebula", "comet",
            "rocket", "orbit", "planet", "constellation"
        }
    };

    // ---------- Entry point ----------

    public void Run()
    {
        while (true)
        {
            var category = ChooseCategory();
            if (category == null) break;

            PlayRound(category);

            if (!AskYesNo("\nPlay again? (y/n) > ")) break;
        }

        Console.WriteLine($"\nFinal record: {_wins} won, {_losses} lost. Thanks for playing!");
    }

    private string? ChooseCategory()
    {
        var names = Categories.Keys.ToList();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== HANGMAN ===");
            Console.WriteLine($"Record: {_wins} won, {_losses} lost\n");
            Console.WriteLine("Choose a category:");
            for (int i = 0; i < names.Count; i++)
                Console.WriteLine($"  {i + 1}. {names[i]}");
            Console.WriteLine("  R. Random");
            Console.WriteLine("  Q. Quit");
            Console.Write("\n> ");

            var input = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (input == null || input == "q") return null;
            if (input == "r") return names[_rng.Next(names.Count)];
            if (int.TryParse(input, out int n) && n >= 1 && n <= names.Count) return names[n - 1];

            Console.WriteLine("Please enter a number from the list, R, or Q. Press Enter to continue.");
            Console.ReadLine();
        }
    }

    // ---------- One round ----------

    private void PlayRound(string category)
    {
        var word = PickWord(category);
        var guessed = new HashSet<char>();
        int wrong = 0;
        bool hintUsed = false;
        string message = "";

        while (true)
        {
            Draw(category, word, guessed, wrong, message);

            if (IsSolved(word, guessed))
            {
                _wins++;
                Console.WriteLine($"\nYou got it! The word was \"{word.ToUpperInvariant()}\".");
                return;
            }

            if (wrong >= MaxWrong)
            {
                _losses++;
                Console.WriteLine($"\nYou lost! The word was \"{word.ToUpperInvariant()}\".");
                return;
            }

            Console.Write("\nGuess a letter or the whole word (? = hint) > ");
            var input = Console.ReadLine();
            if (input == null) return; // input stream closed

            input = input.Trim().ToLowerInvariant();
            message = "";

            if (input.Length == 0) continue;

            // Hint: reveals a random missing letter but costs one life, once per round.
            if (input == "?")
            {
                if (hintUsed)
                {
                    message = "You already used your hint this round.";
                }
                else if (wrong >= MaxWrong - 1)
                {
                    message = "Too risky: a hint costs a life and you only have one left.";
                }
                else
                {
                    var hidden = word.Where(c => !guessed.Contains(c)).Distinct().ToList();
                    var letter = hidden[_rng.Next(hidden.Count)];
                    guessed.Add(letter);
                    wrong++;
                    hintUsed = true;
                    message = $"Hint: the word contains '{char.ToUpperInvariant(letter)}'. (cost 1 life)";
                }
                continue;
            }

            // Single letter
            if (input.Length == 1)
            {
                char c = input[0];

                if (!char.IsAsciiLetter(c))
                {
                    message = "Letters only, please.";
                    continue;
                }

                if (guessed.Contains(c))
                {
                    message = $"You already guessed '{char.ToUpperInvariant(c)}'.";
                    continue;
                }

                guessed.Add(c);
                if (word.Contains(c))
                {
                    message = $"Yes! '{char.ToUpperInvariant(c)}' is in the word.";
                }
                else
                {
                    wrong++;
                    message = $"Sorry, no '{char.ToUpperInvariant(c)}'.";
                }
                continue;
            }

            // Whole-word guess
            if (!input.All(ch => char.IsAsciiLetter(ch)))
            {
                message = "Letters only, please.";
                continue;
            }

            if (input == word)
            {
                guessed.UnionWith(word); // reveals everything; the win check runs next loop
            }
            else
            {
                wrong++;
                message = $"\"{input.ToUpperInvariant()}\" is not the word.";
            }
        }
    }

    private string PickWord(string category)
    {
        var all = Categories[category];
        var pool = all.Where(w => !_used.Contains(w)).ToList();

        if (pool.Count == 0) // played every word in this category: start over
        {
            foreach (var w in all) _used.Remove(w);
            pool = all.ToList();
        }

        var word = pool[_rng.Next(pool.Count)];
        _used.Add(word);
        return word;
    }

    private static bool IsSolved(string word, HashSet<char> guessed) =>
        word.All(c => guessed.Contains(c));

    // ---------- Drawing ----------

    private void Draw(string category, string word, HashSet<char> guessed, int wrong, string message)
    {
        Console.Clear();
        Console.WriteLine($"=== HANGMAN ===   Category: {category}   Record: {_wins}W / {_losses}L\n");
        Console.WriteLine(Gallows(wrong));
        Console.WriteLine();

        var display = string.Join(" ", word.Select(c => guessed.Contains(c) ? char.ToUpperInvariant(c) : '_'));
        Console.WriteLine("  " + display);
        Console.WriteLine();

        var wrongLetters = guessed
            .Where(c => !word.Contains(c))
            .OrderBy(c => c)
            .Select(char.ToUpperInvariant);

        Console.WriteLine($"Wrong letters: {string.Join(" ", wrongLetters)}");
        Console.WriteLine($"Lives left: {MaxWrong - wrong}");

        if (message.Length > 0)
            Console.WriteLine($"\n{message}");
    }

    private static string Gallows(int wrong)
    {
        char head = wrong >= 1 ? 'O' : ' ';
        char body = wrong >= 2 ? '|' : ' ';
        char leftArm = wrong >= 3 ? '/' : ' ';
        char rightArm = wrong >= 4 ? '\\' : ' ';
        char leftLeg = wrong >= 5 ? '/' : ' ';
        char rightLeg = wrong >= 6 ? '\\' : ' ';

        return string.Join(Environment.NewLine,
            "  +---+",
            "  |   |",
            $"  {head}   |",
            $" {leftArm}{body}{rightArm}  |",
            $" {leftLeg} {rightLeg}  |",
            "      |",
            "=========");
    }

    // ---------- Helpers ----------

    private static bool AskYesNo(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (input == null) return false;
            if (input is "y" or "yes") return true;
            if (input is "n" or "no") return false;
        }
    }
}