using System.Collections.Generic;

/// <summary>
/// Generates the starting contract pool at new game start.
/// Pool is biased by company background and founder roles.
/// Same seed + same background + same founders = same pool (deterministic).
/// </summary>
public static class StartingContractGenerator
{
    // Starting difficulty range — low to medium for early game
    private const int MinDifficulty = 1;
    private const int EasyDifficulty = 1;
    private const int MediumDifficulty = 2;

    // Pool size bounds
    private const int MinPoolSize = 3;
    private const int MaxPoolSize = 5;

    public static List<Contract> GenerateStartingContracts(
        StartingContractParams contractParams,
        IRng rng,
        ContractFactory contractFactory,
        int currentTick,
        ref int nextContractId)
    {
        if (contractFactory == null)
            return new List<Contract>();

        int poolSize = rng.Range(MinPoolSize, MaxPoolSize + 1);
        float diffMult = contractParams.DifficultyMultiplier > 0f ? contractParams.DifficultyMultiplier : 1.0f;

        // Determine bias categories from background
        string[] biasCategories = GetBiasCategories(contractParams.Background);

        var contracts = new List<Contract>(poolSize);

        // --- Slot 1: Easy/quick tutorial-friendly contract ---
        {
            string category = PickBiasedCategory(biasCategories, rng);
            int difficulty = EasyDifficulty;
            var c = contractFactory.GenerateContract(currentTick, difficulty, null, category);
            if (c != null)
            {
                c.Id = new ContractId(nextContractId++);
                contracts.Add(c);
            }
        }

        // --- Slot 2: Contract matching founder primary role family ---
        {
            string founderCategory = FindFounderMatchCategory(contractParams.FounderRoles, biasCategories, rng);
            int difficulty = EasyDifficulty;
            var c = contractFactory.GenerateContract(currentTick, difficulty, null, founderCategory);
            if (c != null)
            {
                c.Id = new ContractId(nextContractId++);
                contracts.Add(c);
            }
        }

        // --- Slot 3: Moderate stretch-goal contract ---
        if (poolSize >= 3)
        {
            string category = PickBiasedCategory(biasCategories, rng);
            int difficulty = (int)(MediumDifficulty * diffMult);
            if (difficulty < 1) difficulty = 1;
            var c = contractFactory.GenerateContract(currentTick, difficulty, null, category);
            if (c != null)
            {
                c.Id = new ContractId(nextContractId++);
                contracts.Add(c);
            }
        }

        // --- Fill remaining slots ---
        while (contracts.Count < poolSize)
        {
            string category = PickBiasedCategory(biasCategories, rng);
            int difficulty = rng.Range(EasyDifficulty, MediumDifficulty + 1);
            var c = contractFactory.GenerateContract(currentTick, difficulty, null, category);
            if (c != null)
            {
                c.Id = new ContractId(nextContractId++);
                contracts.Add(c);
            }
            else
            {
                // Factory can't produce for this category — stop filling
                break;
            }
        }

        return contracts;
    }

    // --- Helpers ---

    private static string[] GetBiasCategories(CompanyBackgroundDefinition background)
    {
        // Prefer typed ContractPoolBiasCategories first (new field), fall back to string tags
        if (background != null
            && background.ContractPoolBiasCategories != null
            && background.ContractPoolBiasCategories.Length > 0)
        {
            return background.ContractPoolBiasCategories;
        }

        if (background != null
            && background.ContractPoolBiasTags != null
            && background.ContractPoolBiasTags.Length > 0)
        {
            return background.ContractPoolBiasTags;
        }

        return new string[0];
    }

    private static string PickBiasedCategory(string[] biasCategories, IRng rng)
    {
        if (biasCategories == null || biasCategories.Length == 0)
            return null;

        // 70% chance to pick from bias list, 30% chance for any category
        if (rng.Range(0, 100) < 70)
            return biasCategories[rng.Range(0, biasCategories.Length)];

        return null;
    }

    private static string FindFounderMatchCategory(
        RoleId[] founderRoles,
        string[] biasCategories,
        IRng rng)
    {
        // Return the first bias category that roughly matches the founder's role family
        // This is a heuristic — a background biased toward "Engineering" contracts
        // should match a SoftwareEngineer founder.
        if (biasCategories != null && biasCategories.Length > 0)
            return biasCategories[0];

        return null;
    }
}
