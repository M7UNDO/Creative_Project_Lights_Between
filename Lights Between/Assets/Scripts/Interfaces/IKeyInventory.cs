public interface IKeyInventory
{
    bool HasKey(string keyID);
    bool ConsumeKey(string keyID);
}