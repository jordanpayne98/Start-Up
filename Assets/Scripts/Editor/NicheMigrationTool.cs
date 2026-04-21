using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class NicheMigrationTool
{
    private struct NicheDef
    {
        public string path;
        public ProductNiche nicheId;
        public string displayName;
        public string description;

        public NicheDef(string path, ProductNiche nicheId, string displayName, string description) {
            this.path = path;
            this.nicheId = nicheId;
            this.displayName = displayName;
            this.description = description;
        }
    }

    [MenuItem("StartUp/Tools/Migrate Niche Assets")]
    public static void MigrateNiches() {
        var niches = BuildNicheList();
        int created = 0;
        int updated = 0;

        for (int i = 0; i < niches.Count; i++) {
            var def = niches[i];
            var existing = AssetDatabase.LoadAssetAtPath<ProductNicheDefinition>(def.path);
            if (existing != null) {
                existing.nicheId = def.nicheId;
                existing.displayName = def.displayName;
                existing.description = def.description;
                EditorUtility.SetDirty(existing);
                updated++;
            } else {
                AssetDatabase.DeleteAsset(def.path);
                var asset = ScriptableObject.CreateInstance<ProductNicheDefinition>();
                asset.nicheId = def.nicheId;
                asset.displayName = def.displayName;
                asset.description = def.description;
                AssetDatabase.CreateAsset(asset, def.path);
                created++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[NicheMigration] Done. Created: {created}, Updated: {updated}, Total: {niches.Count}");
    }

    private static List<NicheDef> BuildNicheList() {
        const string root = "Assets/Data/Market/Niches/";
        var list = new List<NicheDef>(22);

        // Operating System niches (3)
        list.Add(new NicheDef(root + "DesktopOS.asset",   ProductNiche.DesktopOS,   "Desktop OS",     "Desktop operating system market"));
        list.Add(new NicheDef(root + "Mobile.asset",      ProductNiche.MobileOS,    "Mobile OS",      "Mobile operating system market"));
        list.Add(new NicheDef(root + "ServerOS.asset",    ProductNiche.ServerOS,    "Server OS",      "Server operating system market"));

        // Video Game niches (13)
        list.Add(new NicheDef(root + "RPG.asset",         ProductNiche.RPG,         "RPG",            "Role-playing games with deep narratives"));
        list.Add(new NicheDef(root + "FPS.asset",         ProductNiche.FPS,         "FPS",            "First-person shooter games"));
        list.Add(new NicheDef(root + "Strategy.asset",    ProductNiche.Strategy,    "Strategy",       "Real-time and turn-based strategy"));
        list.Add(new NicheDef(root + "Puzzle.asset",      ProductNiche.Puzzle,      "Puzzle",         "Puzzle and brain teaser games"));
        list.Add(new NicheDef(root + "Platformer.asset",  ProductNiche.Platformer,  "Platformer",     "Side-scrolling and 3D platformers"));
        list.Add(new NicheDef(root + "Simulation.asset",  ProductNiche.Simulation,  "Simulation",     "Life, business, and sandbox simulations"));
        list.Add(new NicheDef(root + "Racing.asset",      ProductNiche.Racing,      "Racing",         "Racing and driving games"));
        list.Add(new NicheDef(root + "Sports.asset",      ProductNiche.Sports,      "Sports",         "Sports simulation and arcade"));
        list.Add(new NicheDef(root + "Horror.asset",      ProductNiche.Horror,      "Horror",         "Horror and survival horror games"));
        list.Add(new NicheDef(root + "Adventure.asset",   ProductNiche.Adventure,   "Adventure",      "Story-driven adventure games"));
        list.Add(new NicheDef(root + "MMORPG.asset",      ProductNiche.MMORPG,      "MMORPG",         "Massively multiplayer online RPGs"));
        list.Add(new NicheDef(root + "Sandbox.asset",     ProductNiche.Sandbox,     "Sandbox",        "Open-world sandbox games"));
        list.Add(new NicheDef(root + "Fighting.asset",    ProductNiche.Fighting,    "Fighting",       "Fighting and combat games"));

        // Mobile App niches (3)
        list.Add(new NicheDef(root + "AppUtility.asset",       ProductNiche.AppUtility,       "Utility",              "Utility and tool apps"));
        list.Add(new NicheDef(root + "AppSocial.asset",        ProductNiche.AppSocial,        "Social (Mobile)",      "Social networking apps"));
        list.Add(new NicheDef(root + "AppProductivity.asset",  ProductNiche.AppProductivity,  "Productivity (Mobile)", "Productivity and organization apps"));

        // Online Service niches (3)
        list.Add(new NicheDef(root + "CRM.asset",           ProductNiche.CRM,           "CRM",           "Customer relationship management"));
        list.Add(new NicheDef(root + "Analytics.asset",     ProductNiche.Analytics,     "Analytics",     "Data analytics and business intelligence"));
        list.Add(new NicheDef(root + "Communication.asset", ProductNiche.Communication, "Communication", "Team communication and messaging"));

        return list;
    }
}
