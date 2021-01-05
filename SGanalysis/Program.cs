using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGanalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] filePath = Environment.GetCommandLineArgs();
            string dnaFilePath = filePath[1];

            MyBytes dnaBytes = new MyBytes(dnaFilePath);
        }
    }

    public class MyBytes
    {
        /// <summary>
        /// バイナリを格納するバイト型リストを返すプロパティ
        /// </summary>
        private List<byte> Bytes { get; set; } = new List<byte>();

        /// <summary>
        /// 読み取り位置を返すプロパティ
        /// </summary>
        private int Pos { get; set; }

        /// <summary>
        /// 指定されたパスに存在するバイナリを読み込んでインスタンス化する
        /// </summary>
        /// <param name="filePath">バイナリファイルのパス</param>
        public MyBytes(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                Bytes.AddRange(br.ReadBytes((int)fs.Length));
            }
            // 初期位置は先頭
            Pos = 0;
        }

        /// <summary>
        ///現在位置の指定されたオフセット位置から指定されたバイト数だけ読み込む
        /// </summary>
        /// <param name="count">読み込むバイト数</param>
        /// <param name="offsetPos">オフセットするバイト数</param>
        /// <param name="readNext">現在位置を自動的に読み込み終了地点に進める場合にはtrue</param>
        /// <returns></returns>
        public byte[] ReadBytes(int count, int offsetPos = 0, bool readNext = true)
        {
            List<byte> result = Bytes.GetRange(Pos + offsetPos, count);
            if (readNext)
            {
                Pos = Pos + offsetPos + count;
            }

            return result.ToArray();
        }

        /// <summary>
        /// ファイルの終端かどうかを返すプロパティ（読み取り専用）
        /// </summary>
        public bool EOF
        {
            get
            {
                return Bytes.Count <= Pos;
            }
        }
    }
}
