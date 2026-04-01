using System.Windows;

namespace MousePoint.Core;

/// <summary>
/// Douglas-Peucker 알고리즘으로 폴리라인을 간소화한다.
/// 형광펜 스트로크의 포인트 수를 줄여 렌더링 부하를 절감.
/// </summary>
public static class DouglasPeucker
{
    public static List<Point> Simplify(IReadOnlyList<Point> points, double epsilon)
    {
        if (points.Count <= 2)
            return new List<Point>(points);

        // 시작점에서 끝점까지의 직선으로부터 가장 먼 점 탐색
        double maxDist = 0;
        int maxIndex = 0;

        Point first = points[0];
        Point last = points[points.Count - 1];

        for (int i = 1; i < points.Count - 1; i++)
        {
            double dist = PerpendicularDistance(points[i], first, last);
            if (dist > maxDist)
            {
                maxDist = dist;
                maxIndex = i;
            }
        }

        if (maxDist > epsilon)
        {
            // 재귀: 가장 먼 점을 기준으로 분할
            var left = Simplify(Slice(points, 0, maxIndex + 1), epsilon);
            var right = Simplify(Slice(points, maxIndex, points.Count), epsilon);

            // 좌측 결과의 마지막 점(= maxIndex)과 우측의 첫 점이 동일하므로 하나 제거
            var result = new List<Point>(left.Count + right.Count - 1);
            result.AddRange(left);
            for (int i = 1; i < right.Count; i++)
                result.Add(right[i]);

            return result;
        }

        // 모든 중간점이 epsilon 이내 → 양 끝점만 유지
        return [first, last];
    }

    private static double PerpendicularDistance(Point pt, Point lineStart, Point lineEnd)
    {
        double dx = lineEnd.X - lineStart.X;
        double dy = lineEnd.Y - lineStart.Y;

        double lengthSq = dx * dx + dy * dy;
        if (lengthSq == 0)
        {
            // lineStart == lineEnd
            double ex = pt.X - lineStart.X;
            double ey = pt.Y - lineStart.Y;
            return Math.Sqrt(ex * ex + ey * ey);
        }

        double area = Math.Abs(dy * pt.X - dx * pt.Y + lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X);
        return area / Math.Sqrt(lengthSq);
    }

    private static IReadOnlyList<Point> Slice(IReadOnlyList<Point> points, int start, int end)
    {
        var result = new List<Point>(end - start);
        for (int i = start; i < end; i++)
            result.Add(points[i]);
        return result;
    }
}
