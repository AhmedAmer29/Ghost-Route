using UnityEngine;

public class AttachPaper : MonoBehaviour
{
    [Header("Paper")]
    public Transform handBone;
    public Transform paper;

    [Header("Paper Held Position (local to hand)")]
    public Vector3 heldPosition = Vector3.zero;
    public Vector3 heldRotation = Vector3.zero;

    [Header("Photo")]
    public Transform photo;

    [Header("Photo Held Position (local to hand)")]
    public Vector3 photoHeldPosition = Vector3.zero;
    public Vector3 photoHeldRotation = Vector3.zero;

    private Transform paperOriginalParent;
    private Vector3 paperOriginalPosition;
    private Quaternion paperOriginalRotation;
    private Vector3 paperOriginalScale;

    private Transform photoOriginalParent;
    private Vector3 photoOriginalPosition;
    private Quaternion photoOriginalRotation;
    private Vector3 photoOriginalScale;

    void Start()
    {
        if (paper != null)
        {
            paperOriginalParent   = paper.parent;
            paperOriginalPosition = paper.position;
            paperOriginalRotation = paper.rotation;
            paperOriginalScale    = paper.localScale;
        }

        if (photo != null)
        {
            photoOriginalParent   = photo.parent;
            photoOriginalPosition = photo.position;
            photoOriginalRotation = photo.rotation;
            photoOriginalScale    = photo.localScale;
        }
    }

    // ── PAPER ──────────────────────────────────────

    public void Attach()
    {
        if (paper == null) { Debug.LogError("AttachPaper: paper is NULL"); return; }
        if (handBone == null) { Debug.LogError("AttachPaper: handBone is NULL"); return; }

        Vector3 worldScale = paper.lossyScale;
        paper.SetParent(handBone, false);
        paper.localPosition = heldPosition;
        paper.localRotation = Quaternion.Euler(heldRotation);

        Vector3 parentScale = handBone.lossyScale;
        paper.localScale = new Vector3(
            worldScale.x / parentScale.x,
            worldScale.y / parentScale.y,
            worldScale.z / parentScale.z
        );

        Debug.Log("AttachPaper: Paper attached to hand");
    }

    public void Detach()
    {
        if (paper == null) { Debug.LogError("AttachPaper: paper is NULL"); return; }
        paper.SetParent(paperOriginalParent, true);
        paper.position   = paperOriginalPosition;
        paper.rotation   = paperOriginalRotation;
        paper.localScale = paperOriginalScale;
        Debug.Log("AttachPaper: Paper detached and returned to original position");
    }

    // ── PHOTO ──────────────────────────────────────

    public void AttachPhoto()
    {
        if (photo == null) { Debug.LogError("AttachPaper: photo is NULL"); return; }
        if (handBone == null) { Debug.LogError("AttachPaper: handBone is NULL"); return; }

        Vector3 worldScale = photo.lossyScale;
        photo.SetParent(handBone, false);
        photo.localPosition = photoHeldPosition;
        photo.localRotation = Quaternion.Euler(photoHeldRotation);

        Vector3 parentScale = handBone.lossyScale;
        photo.localScale = new Vector3(
            worldScale.x / parentScale.x,
            worldScale.y / parentScale.y,
            worldScale.z / parentScale.z
        );

        Debug.Log("AttachPaper: Photo attached to hand");
    }

    public void DetachPhoto()
    {
        if (photo == null) { Debug.LogError("AttachPaper: photo is NULL"); return; }
        photo.SetParent(photoOriginalParent, true);
        photo.position   = photoOriginalPosition;
        photo.rotation   = photoOriginalRotation;
        photo.localScale = photoOriginalScale;
        Debug.Log("AttachPaper: Photo detached and returned to original position");
    }
}