using System;
using System.Text;

namespace MiyakoCarryService.Client.Misc
{
    public class Matrix5x5
    {
        // 考虑到配置管理器可能保存不了，最终应该不会使用，但是保留此类的代码作为届时的参考
        private readonly int[,] _data = new int[5, 5];

        public Matrix5x5()
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (i == 2 && j == 2)
                    {
                        _data[i, j] = -1;
                    }
                    else
                    {
                        _data[i, j] = 0;
                    }
                }
            }
        }

        // 索引器：支持 matrix[row, col]
        public int this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= 5 || col < 0 || col >= 5)
                {
                    throw new ArgumentOutOfRangeException($"Index ({row}, {col}) out of range. Must be [0,4].");
                }
                return _data[row, col];
            }
            set
            {
                if (value < -1 || value > 4)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"Value must be between -1 and 4 inclusive. Got: {value}");

                }
                if (row < 0 || row >= 5 || col < 0 || col >= 5)
                {
                    throw new ArgumentOutOfRangeException($"Index ({row}, {col}) out of range. Must be [0,4].");
                }

                _data[row, col] = value;
            }
        }

        // 可选：支持元组索引（C# 7+）
        public int this[(int row, int col) index] => this[index.row, index.col];

        // 批量设置（可选）
        public void SetRow(int rowIndex, params int[] values)
        {
            if (rowIndex < 0 || rowIndex >= 5)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            }
            if (values?.Length != 5)
            {
                throw new ArgumentException("Row must have exactly 5 values.");
            }

            for (int j = 0; j < 5; j++)
            {
                this[rowIndex, j] = values[j]; // 自动触发验证
            }
        }

        // 遍历所有元素
        public void ForEach(Action<int, int, int> action)
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    action(_data[i, j], i, j);
                }

            }
        }

        // 转字符串（调试友好）
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    sb.Append(_data[i, j].ToString().PadLeft(3));
                    if (j < 4)
                    {
                        sb.Append(' ');
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        
    }
}