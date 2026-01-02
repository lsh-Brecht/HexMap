using UnityEngine;

public static class HexMetrics {
    //육각형의 외접원 반지름(Outer Radius)에 대한 내접원 반지름(Inner Radius)의 비율
    public const float outerToInner = 0.866025404f; //sqrt(3) / 2
    public const float innerToOuter = 1f / outerToInner;
    public const float innerDiameter = innerRadius * 2f;

    //외접원 반지름의 크기
    public const float outerRadius = 10f;
	//중심에서 변까지의 거리
	public const float innerRadius = outerRadius * outerToInner;

	//색상 interpolation
	public const float solidFactor = 0.8f;
	public const float blendFactor = 1f - solidFactor;

	public const float waterFactor = 0.6f;
	public const float waterBlendFactor = 1f - waterFactor;

	//Elevation 사이의 높이 차이
	public const float elevationStep = 3f;

	//Slope마다 생성되는 계단의 개수
	public const int terracesPerSlope = 2;

	public const int terraceSteps = terracesPerSlope * 2 + 1;

	//계단의 간격 크기
	public const float horizontalTerraceStepSize = 1f / terraceSteps;
	public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

	//셀 정점 위치를 불규칙하게 흔들어주는 강도입니다. 자연스러운 느낌을 주기 위해 사용됩니다.
	public const float cellPerturbStrength = 4f;

	//마찬가지로 Elevation을 불규칙하게 흔드는 강도입니다.
	public const float elevationPerturbStrength = 1.5f;
	//강바닥(Stream Bed)에 사용되는 오프셋
	public const float streamBedElevationOffset = -1.75f;
	//수면(Water Surface)의 사용되는 오프셋
	public const float waterElevationOffset = -0.5f;

	// 성벽 관련 높이, 오프셋, 두께, 타워 배치를 위한 임계값.
	public const float wallHeight = 4f;
	public const float wallYOffset = -1f;
	public const float wallThickness = 0.75f;
	public const float wallElevationOffset = verticalTerraceStepSize;
	public const float wallTowerThreshold = 0.5f;

	public const float bridgeDesignLength = 7f;

	// 노이즈 샘플링 스케일입니다. 노이즈 텍스처를 얼마나 확대/축소해서 읽을지 결정합니다.
	public const float noiseScale = 0.003f;

	//청크 하나에 포함되는 셀의 개수
	public const int chunkSizeX = 5, chunkSizeZ = 5;

	//Hash Grid의 크기입니다.
	public const int hashGridSize = 256;
	public const float hashGridScale = 0.25f;

	static HexHash[] hashGrid;

	//육각형의 6개 코너의 vertex 위치를 정의하는 배열입니다.
	//7번째 값은 첫 번째 값과 같습니다. 그래픽스에서 원을 근사하는 경우와 마찬가지로 하나 더 필요합니다.
	static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
	};

	static float[][] featureThresholds = {
		new float[] {0.0f, 0.0f, 0.4f},
		new float[] {0.0f, 0.4f, 0.6f},
		new float[] {0.4f, 0.6f, 0.8f}
	};

	public static Texture2D noiseSource;

	//주어진 월드 좌표(position)에서 노이즈 값을 샘플링합니다
	//XZ 평면 좌표를 사용하여 텍스처의 픽셀 값을 가져옵니다
	public static Vector4 SampleNoise (Vector3 position) {
		return noiseSource.GetPixelBilinear(
			position.x * noiseScale,
			position.z * noiseScale
		);
	}

	public static void InitializeHashGrid (int seed) {
		hashGrid = new HexHash[hashGridSize * hashGridSize];
		Random.State currentState = Random.state;
		Random.InitState(seed);
		for (int i = 0; i < hashGrid.Length; i++) {
			hashGrid[i] = HexHash.Create();
		}
		Random.state = currentState;
	}

	//주어진 위치의 해시(Hash) 값을 가져옵니다.
	public static HexHash SampleHashGrid (Vector3 position) {
		int x = (int)(position.x * hashGridScale) % hashGridSize;
		if (x < 0) {
			x += hashGridSize;
		}
		int z = (int)(position.z * hashGridScale) % hashGridSize;
		if (z < 0) {
			z += hashGridSize;
		}
		return hashGrid[x + z * hashGridSize];
	}

	public static float[] GetFeatureThresholds (int level) {
		return featureThresholds[level];
	}

	//코너 좌표를 반환합니다.
	public static Vector3 GetFirstCorner (HexDirection direction) {
		return corners[(int)direction];
	}

	public static Vector3 GetSecondCorner (HexDirection direction) {
		return corners[(int)direction + 1];
	}

	public static Vector3 GetFirstSolidCorner (HexDirection direction) {
		return corners[(int)direction] * solidFactor;
	}

	public static Vector3 GetSecondSolidCorner (HexDirection direction) {
		return corners[(int)direction + 1] * solidFactor;
	}

	public static Vector3 GetSolidEdgeMiddle (HexDirection direction) {
		return
			(corners[(int)direction] + corners[(int)direction + 1]) *
			(0.5f * solidFactor);
	}

	public static Vector3 GetFirstWaterCorner (HexDirection direction) {
		return corners[(int)direction] * waterFactor;
	}

	public static Vector3 GetSecondWaterCorner (HexDirection direction) {
		return corners[(int)direction + 1] * waterFactor;
	}

	public static Vector3 GetBridge (HexDirection direction) {
		return (corners[(int)direction] + corners[(int)direction + 1]) *
			blendFactor;
	}

	public static Vector3 GetWaterBridge (HexDirection direction) {
		return (corners[(int)direction] + corners[(int)direction + 1]) *
			waterBlendFactor;
	}

	//Interpolation
	public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
		float h = step * HexMetrics.horizontalTerraceStepSize;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;
		float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
		a.y += (b.y - a.y) * v;
		return a;
	}

	public static Color TerraceLerp (Color a, Color b, int step) {
		float h = step * HexMetrics.horizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	}

	public static Vector3 WallLerp (Vector3 near, Vector3 far) {
		near.x += (far.x - near.x) * 0.5f;
		near.z += (far.z - near.z) * 0.5f;
		float v =
			near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
		near.y += (far.y - near.y) * v + wallYOffset;
		return near;
	}

	public static Vector3 WallThicknessOffset (Vector3 near, Vector3 far) {
		Vector3 offset;
		offset.x = far.x - near.x;
		offset.y = 0f;
		offset.z = far.z - near.z;
		return offset.normalized * (wallThickness * 0.5f);
	}

	//두 셀의 elevation 차이에 따른 엣지 타입(Flat, Slope, Cliff)을 반환합니다.
	public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
		if (elevation1 == elevation2) {
			return HexEdgeType.Flat;
		}
		int delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1) {
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}

	//위치에 노이즈를 적용하여 불규칙하게 만듭니다.
	public static Vector3 Perturb (Vector3 position) {
		Vector4 sample = SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
		position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
		return position;
	}

    public static int wrapSize;

    public static bool Wrapping {
        get {
            return wrapSize > 0;
        }
    }
}