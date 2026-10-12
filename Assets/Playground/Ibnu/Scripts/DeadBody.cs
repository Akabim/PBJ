using UnityEngine;

public class DeadBody : PickupItem
{
    public override string GetPrompt()
    {
        return "Gendong " + gameObject.name;
    }
}