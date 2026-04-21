public enum ProductCategory {
    // Layer 0: Platforms
    OperatingSystem     = 0,
    GameConsole         = 1,
    // Layer 1: Tools
    GameEngine          = 3,
    GraphicsEditor      = 4,
    AudioTool           = 5,
    DevFramework        = 6,
    // Layer 2: Applications
    VideoGame           = 7,
    MobileApp           = 8,
    DesktopSoftware     = 9,
    WebApplication      = 10,
    OnlineService       = 11,
    SecuritySoftware    = 12,
    CloudInfrastructure = 13,
    AIProduct           = 14
}

public static class ProductCategoryExtensions {
    public static int GetLayer(this ProductCategory cat) {
        switch (cat) {
            case ProductCategory.OperatingSystem:
            case ProductCategory.GameConsole:
                return 0;
            case ProductCategory.GameEngine:
            case ProductCategory.GraphicsEditor:
            case ProductCategory.AudioTool:
            case ProductCategory.DevFramework:
                return 1;
            default:
                return 2;
        }
    }

    public static bool IsPlatform(this ProductCategory cat) => cat.GetLayer() == 0;

    public static bool IsTool(this ProductCategory cat) => cat.GetLayer() == 1;

    public static bool IsApplication(this ProductCategory cat) => cat.GetLayer() == 2;

    public static ProductLayer ToProductLayer(this ProductCategory cat) {
        int layer = cat.GetLayer();
        return layer == 0 ? ProductLayer.Platform : layer == 1 ? ProductLayer.Tool : ProductLayer.Application;
    }
}
