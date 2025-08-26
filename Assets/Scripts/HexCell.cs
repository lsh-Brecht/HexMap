using UnityEngine;
using static HexMetrics;

public class HexCell : MonoBehaviour {

    [SerializeField]
    HexCell[] neighbors;
    //Set the size to 6 in prefab inspection

    public HexCoordinates coordinates;
	public Color color;

    public RectTransform uiRect;

    public int Elevation {
        get {
            return elevation;
        }
        set {
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            transform.localPosition = position;

            //UI labels
            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = elevation * -HexMetrics.elevationStep;
            uiRect.localPosition = uiPosition;
        }
    }

    int elevation;

    public HexCell GetNeighbor(HexDirection direction) {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction) {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int)direction].elevation
        );
    }

    public HexEdgeType GetEdgeType(HexCell otherCell) {
        return HexMetrics.GetEdgeType(
            elevation, otherCell.elevation
        );
    }
}