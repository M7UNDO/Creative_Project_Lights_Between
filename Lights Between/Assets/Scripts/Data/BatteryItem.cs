using UnityEngine;

[CreateAssetMenu(fileName = "New Battery Item", menuName = "Inventory/Items/Battery")]
public class BatteryItem : ItemClass
{
    public override bool CanUse(GameObject player)
    {
        if (player == null) return false;

        FlashlightScript flashlight = player.GetComponentInChildren<FlashlightScript>(true);
        if (flashlight == null)
        {
            flashlight = FindFirstObjectByType<FlashlightScript>(FindObjectsInactive.Include);
        }

        return flashlight != null && flashlight.requiresBatteryChange;
    }

    public override void Use(GameObject player)
    {
        if (player == null) return;

        FlashlightScript flashlight = player.GetComponentInChildren<FlashlightScript>(true);
        if (flashlight == null)
        {
            flashlight = FindFirstObjectByType<FlashlightScript>(FindObjectsInactive.Include);
        }

        if (flashlight != null)
        {
            flashlight.ReplaceBatteryFromInventory();
        }
    }
}