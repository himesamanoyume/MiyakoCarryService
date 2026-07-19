namespace MiyakoCarryService.Client.Utils
{
    internal static class McsFormationUtils
    {
        public const int MatrixSize = 7;
        public const int Center = 3;
        public static readonly string DefaultMatrix = BuildDefault();

        private static string BuildDefault()
        {
            var arr = new int[MatrixSize * MatrixSize];
            arr[2 * MatrixSize + 1] = 1;
            arr[2 * MatrixSize + 2] = 2;
            arr[2 * MatrixSize + 4] = 3;
            arr[2 * MatrixSize + 5] = 4;
            return Serialize(arr);
        }

        public static int[] Parse(string raw)
        {
            var arr = new int[MatrixSize * MatrixSize];
            if (string.IsNullOrEmpty(raw))
            {
                return arr;
            }

            var parts = raw.Split(',');
            for (int i = 0; i < arr.Length && i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out var v) && v >= 0 && v <= 4)
                {
                    arr[i] = v;
                }
            }
            return arr;
        }

        public static string Serialize(int[] arr) => string.Join(",", arr);

        public static void SetCell(int[] arr, int index, int value)
        {
            if (index == Center * MatrixSize + Center)
            {
                return;
            }

            if (value < 0 || value > 4 || arr[index] == value)
            {
                return;
            }

            if (value != 0)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (i != index && arr[i] == value)
                    {
                        arr[i] = 0;
                    }
                }
            }
            arr[index] = value;
        }
    }
}