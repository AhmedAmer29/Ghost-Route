/// <summary>
/// Simple static inventory state shared across all interaction scripts.
/// No MonoBehaviour needed — just read/write the properties anywhere.
/// </summary>
public static class PlayerInventory
{
    public static bool HasLadder { get; set; } = false;
}
