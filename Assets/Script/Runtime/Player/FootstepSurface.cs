using UnityEngine;

public enum FootstepSurfaceType { Sand, Stone, Wood }

[DisallowMultipleComponent]
public class FootstepSurface : MonoBehaviour
{
    [SerializeField] private FootstepSurfaceType surface;
    public FootstepSurfaceType Surface => surface;
}
