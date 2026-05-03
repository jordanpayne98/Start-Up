public enum CareerStage
{
    Junior,
    EarlyCareer,
    MidLevel,
    Senior,
    Veteran
}

public struct CareerStageData
{
    public int AgeMin;
    public int AgeMax;
    // CA target band
    public int CAMin;
    public int CAMax;
    // PA margin above CA
    public int PAMarginMin;
    public int PAMarginMax;
    // Salary bias multiplier (1.0 = neutral)
    public float SalaryBias;
}

public static class CareerStageHelper
{
    private static readonly CareerStageData[] _data = new CareerStageData[]
    {
        // Junior: age 18-24, low CA, high PA margin
        new CareerStageData { AgeMin = 18, AgeMax = 24, CAMin = 15,  CAMax = 59,  PAMarginMin = 30, PAMarginMax = 80,  SalaryBias = 0.80f },
        // EarlyCareer: age 22-30, low-med CA
        new CareerStageData { AgeMin = 22, AgeMax = 30, CAMin = 40,  CAMax = 99,  PAMarginMin = 20, PAMarginMax = 60,  SalaryBias = 0.90f },
        // MidLevel: age 27-38, med CA
        new CareerStageData { AgeMin = 27, AgeMax = 38, CAMin = 60,  CAMax = 139, PAMarginMin = 10, PAMarginMax = 45,  SalaryBias = 1.00f },
        // Senior: age 34-50, med-high CA
        new CareerStageData { AgeMin = 34, AgeMax = 50, CAMin = 100, CAMax = 169, PAMarginMin = 0,  PAMarginMax = 30,  SalaryBias = 1.10f },
        // Veteran: age 45-65, high CA, low PA margin
        new CareerStageData { AgeMin = 45, AgeMax = 65, CAMin = 120, CAMax = 200, PAMarginMin = -10, PAMarginMax = 20, SalaryBias = 1.15f },
    };

    public static CareerStageData GetData(CareerStage stage)
    {
        int idx = (int)stage;
        if (idx >= 0 && idx < _data.Length) return _data[idx];
        return _data[0];
    }

    // Derives career stage from age with some variance using the provided rng.
    public static CareerStage FromAge(int age, IRng rng)
    {
        // Base stage from age bands, with slight overlap / variance
        CareerStage base_stage;
        if (age <= 23)      base_stage = CareerStage.Junior;
        else if (age <= 29) base_stage = CareerStage.EarlyCareer;
        else if (age <= 37) base_stage = CareerStage.MidLevel;
        else if (age <= 49) base_stage = CareerStage.Senior;
        else                base_stage = CareerStage.Veteran;

        // 20% chance to be one stage higher or lower (represents early/late bloomers)
        if (rng != null)
        {
            int roll = rng.Range(0, 100);
            if (roll < 10 && base_stage > CareerStage.Junior)
                return base_stage - 1;
            if (roll >= 90 && base_stage < CareerStage.Veteran)
                return base_stage + 1;
        }
        return base_stage;
    }
}
