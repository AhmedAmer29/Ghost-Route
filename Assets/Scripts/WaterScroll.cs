using UnityEngine;

public class WaterScroll : MonoBehaviour
{
    public float scrollSpeedX = 0.5f;
    public float scrollSpeedY = 0.5f;

    private Renderer rend;
    private Material waterMaterial;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            waterMaterial = rend.material;
        }
    }

    void Update()
    {
        if (waterMaterial != null)
        {
            float offsetX = Time.time * scrollSpeedX;
            float offsetY = Time.time * scrollSpeedY;
            waterMaterial.mainTextureOffset = new Vector2(offsetX, offsetY);
        }
    }
}
