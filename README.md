### Unity Hex Map  

Catlike Coding의 Unity Hex Map 튜토리얼로 배우며 구현해 본 간단한 헥사 타일맵 프로젝트.  

procedural mesh generation과 타일맵에서의 경로 탐색 알고리즘을 학습하는 걸 목표로 진행.  

---
경로 탐색 (Pathfinding)

마우스 클릭으로 시작 셀과 도착 셀을 지정하면, 두 지점 사이의 최단 경로를 찾아 표시.    

<img src="images/hexMapScreenShot1.png" width="500">  

다른 유닛과 일부 지형을 피해서 경로를 찾는 예시  
(거리가 아닌 이동에 필요한 턴 표시)  

<img src="images/hexMapScreenShot2.png" width="300">  

간단한 전장의 안개(부드러워 보이는 효과)

<img src="images/hexMapScreenShot3.png" width="300">

간단한 맵 생성(높이에 따라 다른 지형 텍스쳐를 사용하는 원시적인 방법)

<img src="images/hexMapScreenShot4.png" width="500">

시드를 이용한 맵 생성(대륙)

<img src="images/hexMapScreenShot5.png" width="500">

---
🛠️ 향후 계획  
- 자연스러운 path finding을 위한 밸런싱
- 맵 생성(어떻게 더 복잡한 바이옴을 만들 것인가?)
- 유닛마다 다른 시야와 유닛을 이용한 도시, 숲, 도로 등 상호작용
- Built-in 렌더링을 기준으로 작성된 텍스쳐를 새 유니티 버전(URP)에 맞게 수정 필요(가능할지 모르겠음)  
---
📚 참고 자료  
- [Catlike Coding: Hex Map Tutorials](https://catlikecoding.com/)
