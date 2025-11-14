namespace PingTool
{
    static class IEnumerableExtension
    {
        public static double Median(this IEnumerable<double> values)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (!values.Any())
                return 0;

            double result = 0;
            var arr = values.OrderBy(x => x).ToArray();
            var len = arr.Length;
            if (len % 2 == 0)
            {
                result = (arr[(len / 2) - 1] + arr[len / 2]) / 2;
            }
            else
            {
                result = arr[(len - 1) / 2];
            }

            return result;
        }
    }
}
