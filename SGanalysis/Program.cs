using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace SGanalysis
{
    class Program
    {
        /// <summary>
        /// SnapGeneファイルのセグメントを読み込むためのクラス
        /// </summary>
        public class SGSegment
        {
            public Byte SGIdentifier { get; set; }
            public Int32 SGSize { get; set; } 
            public Byte[] SGContent { get; set; }

            public SGSegment(byte identifier, int size, byte[] content)
            {
                SGIdentifier = identifier;
                SGSize = size;
                SGContent = content;
            }
        }

        /// <summary>
        /// SnapGeneファイルからセグメント単位で読み込みます
        /// </summary>
        /// <param name="FileName">SnapGeneファイルパス</param>
        /// <returns>全セグメントを格納したリスト</returns>
        public static List<SGSegment> ReadSegments(string FileName)
        {
            // 全セグメントを格納するリスト
            List<SGSegment> output = new List<SGSegment>();

            using (FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int fileSize = (int)fs.Length;               
                while(br.BaseStream.Position != fileSize)
                {
                    byte id = br.ReadByte();
                    byte[] sizeByteArray = br.ReadBytes(4);
                    Array.Reverse(sizeByteArray);
                    int size = BitConverter.ToInt32(sizeByteArray, 0);
                    byte[] content = br.ReadBytes(size);
                    SGSegment seg = new SGSegment(id, size, content);

                    output.Add(seg);
                }               
            }

            return output;
        }

        static void Main(string[] args)
        {
            string[] filePath = Environment.GetCommandLineArgs();
            string dnaFilePath = filePath[1];

            List<SGSegment> SGsegs = ReadSegments(dnaFilePath);

            foreach (SGSegment seg in SGsegs)
            {
                Console.WriteLine("ID  : {0}", seg.SGIdentifier);
                Console.WriteLine("Size: {0}", seg.SGSize);
            }

            Console.ReadKey();
        }
    }
}