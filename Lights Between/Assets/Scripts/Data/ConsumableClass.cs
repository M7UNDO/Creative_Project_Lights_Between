using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable Item", menuName = "Inventory/Items/Consumable")]
public class ConsumableItem : ItemClass
{
    [Header("Consumable Configuration", order = 0)]
    [Space(5)]
    public ConsumableCategory Category;
    public float ValueAmount = 100f;

    public override bool CanUse(GameObject player)
    {
        if (player == null) return false;

        switch (Category)
        {
            case ConsumableCategory.Battery:
                FlashlightScript flashlight = player.GetComponentInChildren<FlashlightScript>(true);
                if (flashlight == null)
                {
                    flashlight = FindFirstObjectByType<FlashlightScript>(FindObjectsInactive.Include);
                }
                return flashlight != null && flashlight.requiresBatteryChange;

            case ConsumableCategory.KeroseneRefill:
                // Return whether lamp needs fuel
                return true;

            case ConsumableCategory.Key:
                // Keys are used automatically via trigger/interaction context rather than manual inventory usage
                return false;

            default:
                return true;
        }
    }

    public override void Use(GameObject player)
    {
        if (player == null) return;

        switch (Category)
        {
            case ConsumableCategory.Battery:
                FlashlightScript flashlight = player.GetComponentInChildren<FlashlightScript>(true);
                if (flashlight == null)
                {
                    flashlight = FindFirstObjectByType<FlashlightScript>(FindObjectsInactive.Include);
                }
                if (flashlight != null)
                {
                    flashlight.ReplaceBatteryFromInventory();
                }
                break;

            case ConsumableCategory.KeroseneRefill:
                Debug.Log($"Refilled lantern with {ValueAmount} kerosene.");
                break;

            case ConsumableCategory.Key:
                Debug.Log("Key selected. Use this key directly on a locked door.");
                break;

            case ConsumableCategory.Health:
                Debug.Log($"Restored {ValueAmount} health.");
                break;
        }
    }
}