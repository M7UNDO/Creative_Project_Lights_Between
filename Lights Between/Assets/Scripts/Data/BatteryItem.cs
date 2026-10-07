using UnityEngine;

[CreateAssetMenu(fileName = "New Battery Item", menuName = "Inventory/Items/Battery")]
public class BatteryItem : ItemClass
{
    [Header("Battery Settings", order = 0)]
    [Space(5)]
    public float ChargeAmount = 50f;

    public override bool CanUse(GameObject player)
    {
        return true;
    }

    public override void Use(GameObject player)
    {
        Debug.Log($"Used battery to charge flashlight by {ChargeAmount}");
    }
}