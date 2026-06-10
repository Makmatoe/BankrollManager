namespace BankrollManager.App;

internal sealed class NaturalSortComparer : IComparer<string>
{
    public static readonly NaturalSortComparer Instance = new();

    private NaturalSortComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        x ??= string.Empty;
        y ??= string.Empty;
        var xIndex = 0;
        var yIndex = 0;

        while (xIndex < x.Length && yIndex < y.Length)
        {
            if (char.IsDigit(x[xIndex]) && char.IsDigit(y[yIndex]))
            {
                var numberCompare = CompareNumberRuns(x, ref xIndex, y, ref yIndex);
                if (numberCompare != 0)
                {
                    return numberCompare;
                }

                continue;
            }

            var charCompare = char.ToUpperInvariant(x[xIndex]).CompareTo(char.ToUpperInvariant(y[yIndex]));
            if (charCompare != 0)
            {
                return charCompare;
            }

            xIndex++;
            yIndex++;
        }

        return x.Length.CompareTo(y.Length);
    }

    private static int CompareNumberRuns(string x, ref int xIndex, string y, ref int yIndex)
    {
        var xStart = xIndex;
        var yStart = yIndex;
        while (xIndex < x.Length && char.IsDigit(x[xIndex]))
        {
            xIndex++;
        }

        while (yIndex < y.Length && char.IsDigit(y[yIndex]))
        {
            yIndex++;
        }

        var xRun = x[xStart..xIndex].TrimStart('0');
        var yRun = y[yStart..yIndex].TrimStart('0');
        xRun = xRun.Length == 0 ? "0" : xRun;
        yRun = yRun.Length == 0 ? "0" : yRun;
        var lengthCompare = xRun.Length.CompareTo(yRun.Length);
        if (lengthCompare != 0)
        {
            return lengthCompare;
        }

        var valueCompare = string.Compare(xRun, yRun, StringComparison.Ordinal);
        if (valueCompare != 0)
        {
            return valueCompare;
        }

        return x[xStart..xIndex].Length.CompareTo(y[yStart..yIndex].Length);
    }
}
