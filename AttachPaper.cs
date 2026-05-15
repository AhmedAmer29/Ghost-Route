using UnityEngine;

public class AttachPaper : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Transform handBone;      // Drag RightHand bone here
    public Transform paper;         // Drag the paper GameObject here
    public Transform paperRestPos;  // Drag an empty GameObject placed at desk position here

    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Transform originalParent;

    void Start()
    {
        if (paper != null)
        {
            originalLocalPos = paper.localPosition;
            originalLocalRot = paper.localRotation;
            originalParent = paper.parent;
        }
    }

    // Call this via Timeline Signal when hand touches paper
    public void Attach()
    {
        if (paper != null && handBone != null)
        {
            paper.SetParent(handBone);
        }
    }

    // Call this via Timeline Signal when paper is placed back on desk
    public void Detach()
    {
        if (paper != null)
        {
            paper.SetParent(originalParent);

            // Snap paper back to its original desk position
            if (paperRestPos != null)
            {
                paper.position = paperRestPos.position;
                paper.rotation = paperRestPos.rotation;
            }
        }
    }
}
