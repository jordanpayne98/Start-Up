using System;

/// <summary>
/// Temporary state container for the new game creation wizard.
/// Accumulates all player selections across wizard steps.
/// Only becomes persistent game state on final confirmation.
/// </summary>
[Serializable]
public class NewGameSetupState
{
    public int Seed;
    public int DifficultyId;
    public string CompanyName = "";
    public string Industry = "";
    public string Location = "";
    public int CompanyBackgroundId = -1;
    public int FounderCount;
    public FounderSetupData[] FounderSetups = Array.Empty<FounderSetupData>();
    public StartingCompanyPreview Preview;
    public WizardValidationState Validation;

    public void InitializeFounderSetups(int count)
    {
        FounderCount = count;
        FounderSetups = new FounderSetupData[count];
        for (int i = 0; i < count; i++)
        {
            FounderSetups[i] = new FounderSetupData
            {
                FounderIndex = i,
                Name = "",
                Age = 30,
                Gender = "",
                PortraitId = -1,
                ArchetypeId = -1,
                RoleId = RoleId.SoftwareEngineer,
                PersonalityStyleId = -1,
                WeaknessId = -1,
                SalaryOptionId = 2,
                CAStars = 0,
                PAStars = 0,
                GeneratedSkillsPreview = Array.Empty<int>(),
                GeneratedVisibleAttributesPreview = Array.Empty<int>(),
                GeneratedHiddenSignalsPreview = Array.Empty<int>()
            };
        }
    }

    [Serializable]
    public struct FounderSetupData
    {
        public int FounderIndex;
        public string Name;
        public int Age;
        public string Gender;
        public int PortraitId;
        public int ArchetypeId;
        public RoleId RoleId;

        /// <summary>
        /// Selected personality style from 8 card options per Page 13 §10.
        /// Index maps to PersonalityStyleDefinition.
        /// </summary>
        public int PersonalityStyleId;

        /// <summary>
        /// Selected founder weakness from 10 card options per Page 13 §11.
        /// Index maps to FounderWeaknessDefinition.
        /// </summary>
        public int WeaknessId;

        public int SalaryOptionId;
        public int[] GeneratedSkillsPreview;
        public int[] GeneratedVisibleAttributesPreview;
        public int[] GeneratedHiddenSignalsPreview;
        public int CAStars;
        public int PAStars;
    }

    [Serializable]
    public struct StartingCompanyPreview
    {
        public string CompanyName;
        public string Industry;
        public string BusinessModel;
        public string Headquarters;
        public int StartingCash;
        public int MonthlyFounderSalaryCost;
        public int EstimatedRunway;
        public float[] FoundingTeamMeters;
        public string[] Strengths;
        public string[] Risks;
        public string[] SuggestedFirstActions;
        public string CandidatePoolBiasSummary;
        public string ContractPoolBiasSummary;
    }

    [Serializable]
    public struct WizardValidationState
    {
        public bool IsValid;
        public string[] BlockingErrors;
        public string[] Warnings;
    }
}
