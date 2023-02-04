using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMapMechanics : MonoBehaviour
{
    float worldToMapScaleRatio = 10f;
    [SerializeField] Transform player;
    [SerializeField] Transform WorldRef3DPoint1;
    [SerializeField] Transform WorldRef3DPoint2;
    [SerializeField] RectTransform MapRefPoint1;
    [SerializeField] RectTransform MapRefPoint2;
    [SerializeField] private RectTransform mapTransform;

    private Vector3 mapPosition = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        Vector2 WorldPoint1 = TwoDify(WorldRef3DPoint1.position);
        Vector2 WorldPoint2 = TwoDify(WorldRef3DPoint2.position);

        worldToMapScaleRatio = (MapRefPoint1.anchoredPosition - MapRefPoint2.anchoredPosition).magnitude / (WorldPoint1 - WorldPoint2).magnitude;
    }
    // Update is called once per frame
    void Update()
    {
        mapPosition = MapRefPoint1.anchoredPosition - worldToMapScaleRatio * TwoDify(WorldRef3DPoint1.position - player.position);
        mapTransform.anchoredPosition = -mapPosition;
    }

    Vector2 TwoDify(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }
}
