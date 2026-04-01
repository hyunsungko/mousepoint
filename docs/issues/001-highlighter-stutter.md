# Issue #001: 형광펜 연속 드래그 시 버벅임

## 현상
형광펜 모드에서 마우스를 떼었다 눌렀다 반복하며 빠르게 여러 스트로크를 그리면
(예: 이름 "고현성" 필기), 뒤쪽 획에서 형광펜이 마우스를 따라가는 속도가 눈에 띄게 느려짐.

마우스 이동 자체는 문제 없고, PathGeometry 렌더링이 지연됨.

## 재현 조건
1. 형광펜 모드 진입 (F9 → 사이드 버튼 1)
2. 동그라미 또는 글자를 10+ 획 빠르게 연속으로 그림
3. 5-6번째 획부터 스트로크가 마우스를 따라가지 못하고 끊김

## 시도한 최적화 (효과 부족)
- PathGeometry → StreamGeometry: 실시간 렌더링 안 됨 (mouse up까지 invisible)
- RenderTargetBitmap 래스터화: 크래시 발생
- Brush.Freeze(): 미미한 효과
- BitmapCache on completed Path: 미미한 효과
- 2-layer Canvas 분리 (ActiveCanvas/OverlayCanvas): 미미한 효과
- 마우스 이동 코얼레싱 (프레임당 1회 dispatch): 약간 개선
- FadeOutManager 60fps → 30fps DispatcherTimer: 약간 개선
- 최소 거리 필터링 (2px): 약간 개선

## 추정 근본 원인
WPF의 AllowsTransparency=True 레이어드 윈도우에서 Canvas 자식 요소가 늘어날수록
합성(compositing) 비용이 비선형 증가. PathGeometry에 LineSegment를 추가할 때마다
WPF가 전체 visual tree를 dirty로 마킹하여 재합성.

## v0.2 해결 방안 후보
1. **DirectComposition / D3DImage**: WPF 렌더링 우회, GPU 직접 합성
2. **WriteableBitmap 단일 레이어**: 모든 스트로크를 픽셀 버퍼에 직접 렌더링
3. **SkiaSharp + SKElement**: WPF Canvas 대신 Skia 렌더러 사용
4. **스트로크 수 제한**: 최대 동시 스트로크 수를 5-6개로 제한, 초과 시 즉시 fade
5. **포인트 간소화**: Douglas-Peucker 알고리즘으로 완성 스트로크 세그먼트 수 감소

## 우선순위
Medium — 일상적 사용(짧은 강조 1-2획)에서는 문제 없음. 빠른 필기 시에만 발생.
