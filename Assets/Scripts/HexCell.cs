using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

	public RectTransform uiRect;

	public HexGridChunk chunk;

    //지형의 높이를 관리합니다.
    //외부에서는 Elevation이라는 속성으로 접근하도록 제한.
    //내부적으로는 elevation 필드를 사용.
    //왜why. Elevation을 변경할 때마다 메쉬 갱신, 위치 갱신, 강과 도로의 유효성 검사 등을 수행해야 하기 때문.
    public int Elevation {
		get {
			return elevation;
		}
		set {
			if (elevation == value) {
				return;
			}
            int originalViewElevation = ViewElevation;
            elevation = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }
            RefreshPosition();
			ValidateRivers(); // 높이 변경으로 인해 강이 거슬러 흐르게 되면 제거합니다.

			for (int i = 0; i < roads.Length; i++) {
				if (roads[i] && GetElevationDifference((HexDirection)i) > 1) {
					SetRoad(i, false); // 높이 차이가 너무 커지면 도로를 끊습니다.
				}
			}

			Refresh();
		}
	}

    //수위를 관리합니다.
	public int WaterLevel {
		get {
			return waterLevel;
		}
		set {
			if (waterLevel == value) {
				return;
			}
            int originalViewElevation = ViewElevation;
            waterLevel = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }
            ValidateRivers();
			Refresh();
		}
	}

    //현재 셀이 물에 잠겨있는지 여부.
    //이런 방식으로 굳이 구현한 이유 : cell.IsUnderwater = true; 같은 코드가 불가능해지며 모호함이 줄어든다.
    public bool IsUnderwater {
		get {
			return waterLevel > elevation;
		}
	}

	public bool HasIncomingRiver {
		get {
			return hasIncomingRiver;
		}
	}

	public bool HasOutgoingRiver {
		get {
			return hasOutgoingRiver;
		}
	}

	public bool HasRiver {
		get {
			return hasIncomingRiver || hasOutgoingRiver;
		}
	}

	public bool HasRiverBeginOrEnd {
		get {
			return hasIncomingRiver != hasOutgoingRiver;
		}
	}

	public HexDirection RiverBeginOrEndDirection {
		get {
			return hasIncomingRiver ? incomingRiver : outgoingRiver;
		}
	}

	public bool HasRoads {
		get {
			for (int i = 0; i < roads.Length; i++) {
				if (roads[i]) {
					return true;
				}
			}
			return false;
		}
	}

	public HexDirection IncomingRiver {
		get {
			return incomingRiver;
		}
	}

	public HexDirection OutgoingRiver {
		get {
			return outgoingRiver;
		}
	}

	public Vector3 Position {
		get {
			return transform.localPosition;
		}
	}


	public float StreamBedY {
		get {
			return
				(elevation + HexMetrics.streamBedElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	public float RiverSurfaceY {
		get {
			return
				(elevation + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	public float WaterSurfaceY {
		get {
			return
				(waterLevel + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

    //도시, 농장, 식물 등 맵의 장식 요소들을 관리하는 속성입니다.
	public int UrbanLevel {
		get {
			return urbanLevel;
		}
		set {
			if (urbanLevel != value) {
				urbanLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int FarmLevel {
		get {
			return farmLevel;
		}
		set {
			if (farmLevel != value) {
				farmLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int PlantLevel {
		get {
			return plantLevel;
		}
		set {
			if (plantLevel != value) {
				plantLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int SpecialIndex {
		get {
			return specialIndex;
		}
		set {
			if (specialIndex != value && !HasRiver) {
				specialIndex = value;
				RemoveRoads();
				RefreshSelfOnly();
			}
		}
	}

	public bool IsSpecial {
		get {
			return specialIndex > 0;
		}
	}

	public bool Walled {
		get {
			return walled;
		}
		set {
			if (walled != value) {
				walled = value;
				Refresh();
			}
		}
	}

	public int TerrainTypeIndex {
		get {
			return terrainTypeIndex;
		}
		set {
			if (terrainTypeIndex != value) {
				terrainTypeIndex = value;
                ShaderData.RefreshTerrain(this);
            }
		}
	}

    //Pathfinding 알고리즘에서 사용되는 속성들입니다.
	public int Distance {
		get {
			return distance;
		}
		set {
			distance = value;
		}
	}

	public HexCell PathFrom { get; set; }

	public int SearchHeuristic { get; set; }

	public int SearchPriority {
		get {
			return distance + SearchHeuristic;
		}
	}

	public HexCell NextWithSamePriority { get; set; }

	int terrainTypeIndex;

	int elevation = int.MinValue;
	int waterLevel;

	int urbanLevel, farmLevel, plantLevel;

	int specialIndex;

	int distance;

	bool walled;

	bool hasIncomingRiver, hasOutgoingRiver;
	HexDirection incomingRiver, outgoingRiver;

	[SerializeField]
	HexCell[] neighbors;

	[SerializeField]
	bool[] roads;

    //특정 방향의 이웃 셀을 반환합니다.
	public HexCell GetNeighbor (HexDirection direction) {
		return neighbors[(int)direction];
	}

    //이웃 관계를 설정합니다.
	public void SetNeighbor (HexDirection direction, HexCell cell) {
		neighbors[(int)direction] = cell;
		cell.neighbors[(int)direction.Opposite()] = this;
	}

    //이웃과의 고도 차이에 따른 경계 타입(평지, 경사, 절벽)을 반환합니다.
	public HexEdgeType GetEdgeType (HexDirection direction) {
        //같으면 Flat, 1 차이면 Slope, 2 이상 차이면 Cliff
        return HexMetrics.GetEdgeType(
			elevation, neighbors[(int)direction].elevation
		);
	}

	public HexEdgeType GetEdgeType (HexCell otherCell) {
		return HexMetrics.GetEdgeType(
			elevation, otherCell.elevation
		);
	}

	public bool HasRiverThroughEdge (HexDirection direction) {
		return
			hasIncomingRiver && incomingRiver == direction ||
			hasOutgoingRiver && outgoingRiver == direction;
	}

	public void RemoveIncomingRiver () {
		if (!hasIncomingRiver) {
			return;
		}
		hasIncomingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(incomingRiver);
		neighbor.hasOutgoingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	public void RemoveOutgoingRiver () {
		if (!hasOutgoingRiver) {
			return;
		}
		hasOutgoingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(outgoingRiver);
		neighbor.hasIncomingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	public void RemoveRiver () {
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

    //강이 흐르는 방향을 설정합니다. 지형 조건이 맞지 않으면 중단하거나 기존 강을 수정합니다.
	public void SetOutgoingRiver (HexDirection direction) {
		if (hasOutgoingRiver && outgoingRiver == direction) {
			return;
		}

		HexCell neighbor = GetNeighbor(direction);
		if (!IsValidRiverDestination(neighbor)) {
			return;
		}

		RemoveOutgoingRiver();
		if (hasIncomingRiver && incomingRiver == direction) {
			RemoveIncomingRiver();
		}
		hasOutgoingRiver = true;
		outgoingRiver = direction;
		specialIndex = 0;

		neighbor.RemoveIncomingRiver();
		neighbor.hasIncomingRiver = true;
		neighbor.incomingRiver = direction.Opposite();
		neighbor.specialIndex = 0;

		SetRoad((int)direction, false);
	}

	public bool HasRoadThroughEdge (HexDirection direction) {
		return roads[(int)direction];
	}

    //도로를 추가합니다. 강이 있거나 고도 차가 너무 크면 수정하지 않습니다.
	public void AddRoad (HexDirection direction) {
		if (
			!roads[(int)direction] && !HasRiverThroughEdge(direction) &&
			!IsSpecial && !GetNeighbor(direction).IsSpecial &&
			GetElevationDifference(direction) <= 1
		) {
			SetRoad((int)direction, true);
		}
	}

	public void RemoveRoads () {
		for (int i = 0; i < neighbors.Length; i++) {
			if (roads[i]) {
				SetRoad(i, false);
			}
		}
	}

	public int GetElevationDifference (HexDirection direction) {
		int difference = elevation - GetNeighbor(direction).elevation;
		return difference >= 0 ? difference : -difference;
	}

	bool IsValidRiverDestination (HexCell neighbor) {
		return neighbor && (
			elevation >= neighbor.elevation || waterLevel == neighbor.elevation
		);
	}

	void ValidateRivers () {
		if (
			hasOutgoingRiver &&
			!IsValidRiverDestination(GetNeighbor(outgoingRiver))
		) {
			RemoveOutgoingRiver();
		}
		if (
			hasIncomingRiver &&
			!GetNeighbor(incomingRiver).IsValidRiverDestination(this)
		) {
			RemoveIncomingRiver();
		}
	}

	void SetRoad (int index, bool state) {
		roads[index] = state;
		neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
		neighbors[index].RefreshSelfOnly();
		RefreshSelfOnly();
	}

    //셀의 3D 월드 좌표를 갱신합니다.
	//노이즈를 적용하여 자연스럽게 보이도록 위치를 잡습니다.
	//위에서 수직으로 내려봤을 때 약간 삐뚤빼뚤하게 보이게 됩니다.
	void RefreshPosition () {
		Vector3 position = transform.localPosition;
		position.y = elevation * HexMetrics.elevationStep;
		position.y +=
			(HexMetrics.SampleNoise(position).y * 2f - 1f) *
			HexMetrics.elevationPerturbStrength;
		transform.localPosition = position;

		Vector3 uiPosition = uiRect.localPosition;
		uiPosition.z = -position.y;
		uiRect.localPosition = uiPosition;
	}

    //셀의 상태가 변경되었을 때 청크를 갱신하여 메쉬를 다시 그리게 합니다.
	void Refresh () {
		if (chunk) {
			chunk.Refresh();
			for (int i = 0; i < neighbors.Length; i++) {
				HexCell neighbor = neighbors[i];
				if (neighbor != null && neighbor.chunk != chunk) {
					neighbor.chunk.Refresh();
				}
			}
            if (Unit) {
                Unit.ValidateLocation();
            }
        }
	}

	void RefreshSelfOnly () {
		chunk.Refresh();
        if (Unit) {
            Unit.ValidateLocation();
        }
    }

    //셀 데이터를 바이너리로 save.
	public void Save (BinaryWriter writer) {
		writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)(elevation + 127));
		writer.Write((byte)waterLevel);
		writer.Write((byte)urbanLevel);
		writer.Write((byte)farmLevel);
		writer.Write((byte)plantLevel);
		writer.Write((byte)specialIndex);
		writer.Write(walled);

		if (hasIncomingRiver) {
			writer.Write((byte)(incomingRiver + 128));
		}
		else {
			writer.Write((byte)0);
		}

		if (hasOutgoingRiver) {
			writer.Write((byte)(outgoingRiver + 128));
		}
		else {
			writer.Write((byte)0);
		}

		int roadFlags = 0;
		for (int i = 0; i < roads.Length; i++) {
			if (roads[i]) {
				roadFlags |= 1 << i;
			}
		}
		writer.Write((byte)roadFlags);
        writer.Write(IsExplored);
    }

    //바이너리 데이터로부터 셀 정보를 load.
	public void Load (BinaryReader reader, int header) {
		terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        elevation = reader.ReadByte();
        if (header >= 4) {
            elevation -= 127;
        }
        RefreshPosition();
		waterLevel = reader.ReadByte();
		urbanLevel = reader.ReadByte();
		farmLevel = reader.ReadByte();
		plantLevel = reader.ReadByte();
		specialIndex = reader.ReadByte();
		walled = reader.ReadBoolean();

		byte riverData = reader.ReadByte();
		if (riverData >= 128) {
			hasIncomingRiver = true;
			incomingRiver = (HexDirection)(riverData - 128);
		}
		else {
			hasIncomingRiver = false;
		}

		riverData = reader.ReadByte();
		if (riverData >= 128) {
			hasOutgoingRiver = true;
			outgoingRiver = (HexDirection)(riverData - 128);
		}
		else {
			hasOutgoingRiver = false;
		}

		int roadFlags = reader.ReadByte();
		for (int i = 0; i < roads.Length; i++) {
			roads[i] = (roadFlags & (1 << i)) != 0;
		}

        IsExplored = header >= 3 ? reader.ReadBoolean() : false;
        ShaderData.RefreshVisibility(this);
    }

	public void SetLabel (string text) {
		UnityEngine.UI.Text label = uiRect.GetComponent<Text>();
		label.text = text;
	}

	public void DisableHighlight () {
		Image highlight = uiRect.GetChild(0).GetComponent<Image>();
		highlight.enabled = false;
	}

	public void EnableHighlight (Color color) {
		Image highlight = uiRect.GetChild(0).GetComponent<Image>();
		highlight.color = color;
		highlight.enabled = true;
	}

    public int SearchPhase { get; set; }

    public HexUnit Unit { get; set; }

    public HexCellShaderData ShaderData { get; set; }

    public int Index { get; set; }

    //현재 셀이 시야에 들어와 있는지 여부를 반환합니다.
    public bool IsVisible {
        get {
            return visibility > 0 && Explorable;
        }
    }

	int visibility;

    //Fog of War를 처리합니다.
    public void IncreaseVisibility() {
        visibility += 1;
        if (visibility == 1) {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility() {
        visibility -= 1;
        if (visibility == 0) {
            ShaderData.RefreshVisibility(this);
        }
    }

    public bool IsExplored {
        get {
            return explored && Explorable;
        }
        private set {
            explored = value;
        }
    }

    //시야 계산에 사용되는 높이입니다. 물에 잠겨있다면 수면 높이를 반환합니다.
    public int ViewElevation {
        get {
            return elevation >= waterLevel ? elevation : waterLevel;
        }
    }

    public void ResetVisibility() {
        if (visibility > 0) {
            visibility = 0;
            ShaderData.RefreshVisibility(this);
        }
    }
    public bool Explorable { get; set; }

    bool explored;

    public void SetMapData(float data) {
        ShaderData.SetMapData(this, data);
    }
}