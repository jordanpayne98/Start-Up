public interface IModifierProvider
{
    float GetMultiplier(string channel);
    float GetAdditive(string channel);
    int GetOverride(string channel, int defaultValue);
}
