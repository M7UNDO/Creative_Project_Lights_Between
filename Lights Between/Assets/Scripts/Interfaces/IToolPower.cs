public interface IToolPower
{
    float CurrentPower { get; }
    float MaxPower { get; }
    bool UsesPower { get; }
}