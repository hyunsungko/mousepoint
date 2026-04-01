using System.Windows;
using MousePoint.Core;
using Xunit;

namespace MousePoint.Tests.Core;

public class DouglasPeuckerTests
{
    [Fact]
    public void 빈_리스트()
    {
        var result = DouglasPeucker.Simplify(new List<Point>(), 1.0);
        Assert.Empty(result);
    }

    [Fact]
    public void 단일점()
    {
        var points = new List<Point> { new(1, 1) };
        var result = DouglasPeucker.Simplify(points, 1.0);
        Assert.Single(result);
        Assert.Equal(new Point(1, 1), result[0]);
    }

    [Fact]
    public void 두점()
    {
        var points = new List<Point> { new(0, 0), new(10, 10) };
        var result = DouglasPeucker.Simplify(points, 1.0);
        Assert.Equal(2, result.Count);
        Assert.Equal(new Point(0, 0), result[0]);
        Assert.Equal(new Point(10, 10), result[1]);
    }

    [Fact]
    public void 직선상_세점_양끝만남음()
    {
        // 세 점이 일직선 → 중간 점 제거
        var points = new List<Point> { new(0, 0), new(5, 5), new(10, 10) };
        var result = DouglasPeucker.Simplify(points, 1.0);
        Assert.Equal(2, result.Count);
        Assert.Equal(new Point(0, 0), result[0]);
        Assert.Equal(new Point(10, 10), result[1]);
    }

    [Fact]
    public void 직각삼각형_세점_모두유지()
    {
        // (0,0)→(10,0)→(10,10): 중간점이 직선에서 충분히 떨어져 있음
        var points = new List<Point> { new(0, 0), new(10, 0), new(10, 10) };
        var result = DouglasPeucker.Simplify(points, 1.0);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void 지그재그_큰epsilon()
    {
        // 지그재그 패턴 + 큰 epsilon → 양 끝점만 남음
        var points = new List<Point>
        {
            new(0, 0), new(1, 1), new(2, 0), new(3, 1), new(4, 0)
        };
        var result = DouglasPeucker.Simplify(points, 5.0);
        Assert.Equal(2, result.Count);
        Assert.Equal(new Point(0, 0), result[0]);
        Assert.Equal(new Point(4, 0), result[^1]);
    }

    [Fact]
    public void 지그재그_작은epsilon()
    {
        // 지그재그 패턴 + 작은 epsilon → 대부분 유지
        var points = new List<Point>
        {
            new(0, 0), new(1, 5), new(2, 0), new(3, 5), new(4, 0)
        };
        var result = DouglasPeucker.Simplify(points, 0.01);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void epsilon_0()
    {
        // epsilon=0이면 모든 점 유지 (직선상 점 제외)
        var points = new List<Point>
        {
            new(0, 0), new(5, 3), new(10, 0)
        };
        var result = DouglasPeucker.Simplify(points, 0.0);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void 큰epsilon()
    {
        // 아무리 복잡해도 극단적으로 큰 epsilon → 양 끝만
        var points = new List<Point>
        {
            new(0, 0), new(100, 200), new(50, -100), new(200, 300), new(400, 0)
        };
        var result = DouglasPeucker.Simplify(points, 10000.0);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void 원본_변경없음()
    {
        var original = new List<Point>
        {
            new(0, 0), new(5, 10), new(10, 0), new(15, 10), new(20, 0)
        };
        var copy = new List<Point>(original);

        DouglasPeucker.Simplify(original, 1.0);

        // 원본 리스트가 변경되지 않았는지 확인
        Assert.Equal(copy.Count, original.Count);
        for (int i = 0; i < copy.Count; i++)
            Assert.Equal(copy[i], original[i]);
    }

    [Fact]
    public void 실제스트로크_포인트수감소()
    {
        // 실제 형광펜 드래그 시뮬레이션: 약간의 노이즈가 있는 대각선
        var points = new List<Point>();
        for (int i = 0; i < 100; i++)
        {
            double noise = (i % 3 == 0) ? 0.5 : (i % 3 == 1) ? -0.3 : 0.1;
            points.Add(new Point(i * 2, i * 2 + noise));
        }

        var result = DouglasPeucker.Simplify(points, 2.0);

        // 간소화 후 포인트 수가 줄어야 함
        Assert.True(result.Count < points.Count, $"간소화 후 {result.Count}개, 원본 {points.Count}개");
        // 첫 점과 끝 점은 유지
        Assert.Equal(points[0], result[0]);
        Assert.Equal(points[^1], result[^1]);
    }
}
