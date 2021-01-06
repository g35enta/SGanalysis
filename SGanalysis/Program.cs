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
            public Byte SGSeqType { get; set; }
            public Byte[] SGSeq { get; set; }

            public SGSegment() { }
            public SGSegment(byte identifier, int size, byte[] content)
            {
                SGIdentifier = identifier;
                SGSize = size;
                SGContent = content;
            }
            public SGSegment(byte identifier, int size, byte[] content, byte type, byte[] seq)
            {
                SGIdentifier = identifier;
                SGSize = size;
                SGContent = content;
                SGSeqType = type;
                SGSeq = seq;
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

                    SGSegment seg = new SGSegment();

                    if (id != 0)
                    {
                        byte[] content = br.ReadBytes(size);
                        seg = new SGSegment(id, size, content);
                    }
                    else
                    {
                        byte[] content = br.ReadBytes(size);
                        byte type = content[0];
                        byte[] seq = new byte[content.Length - 1];
                        Array.Copy(content, 1, seq, 0, content.Length - 1);
                        seg = new SGSegment(id, size, content, type, seq);
                    }

                    output.Add(seg);
                }               
            }

            return output;
        }

        /// <summary>
        /// SnapGeneファイルを作成します
        /// </summary>
        /// <param name="SGsegs">SnapGeneセグメントを格納したリスト</param>
        /// <param name="FileName">保存先のファイルパス</param>
        /// <returns>ファイルが作成されたらtrueを返します</returns>
        public static bool SaveSGFiles(List<SGSegment> SGsegs, string FileName)
        {
            if (SGsegs == null || FileName == "") return false;

            using (FileStream fs = new FileStream(FileName, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                foreach (SGSegment seg in SGsegs)
                {
                    bw.Write(seg.SGIdentifier);
                    byte[] sizeByteArray = BitConverter.GetBytes(seg.SGSize); // LittleEndian
                    Array.Reverse(sizeByteArray); // BigEndian
                    bw.Write(sizeByteArray);
                    bw.Write(seg.SGContent);
                }
            }
            return true;
        }

        /// <summary>
        /// DNA配列フラグを文字列に変換して返す
        /// </summary>
        /// <param name="type">DNA配列フラグを格納したバイト型変数</param>
        /// <returns>変換後の文字列</returns>
        public static string TypeString(byte type)
        {
            string output = "";
            if ((type & 1) != 0)
            {
                output += "Circular, ";
            }
            else
            {
                output += "Linear, ";
            }
            if ((type & 2) != 0)
            {
                output += "Double-stranded, ";
            }
            else
            {
                output += "Single-stranded, ";
            }
            if ((type & 4) != 0)
            {
                output += "dam-methylated, ";
            }
            else
            {
                output += "dam-non-methylated, ";
            }
            if ((type & 8) != 0)
            {
                output += "dcm-methylated, ";
            }
            else
            {
                output += "dcm-non-methylated, ";
            }
            if ((type & 16) != 0)
            {
                output += "ecoKI-methylated, ";
            }
            else
            {
                output += "ecoKI-non-methylated, ";
            }

            return output;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine("  SnapGeneファイル編集ツール");
            Console.WriteLine("    Copyright (c) 2021 Genta Ito");
            Console.WriteLine("    Version 1.0");
            Console.WriteLine("======================================================");
            Console.WriteLine("");

            string[] filePath = Environment.GetCommandLineArgs();
            string dnaFilePath = filePath[1];

            List<SGSegment> SGsegs = new List<SGSegment>();

            try
            {
                SGsegs = ReadSegments(dnaFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("   エラーが発生しました。");
                Console.WriteLine(ex.Message);
                Console.ReadKey();
                Environment.Exit(0);
            }

            if (SGsegs[0].SGIdentifier != 9)
            {
                Console.WriteLine("   SnapGeneファイルを読み込めません。");
                Console.WriteLine("   ファイル形式を確認してください。");
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.WriteLine("   セグメント数: {0}", SGsegs.Count.ToString());

            foreach (SGSegment seg in SGsegs)
            {
                if (seg.SGIdentifier != 0)
                {
                    Console.WriteLine("   ID     : {0}", seg.SGIdentifier.ToString());
                    Console.WriteLine("   Size   : {0}", seg.SGSize.ToString());
                    Console.WriteLine("   Content:");
                    Console.WriteLine(System.Text.Encoding.UTF8.GetString(seg.SGContent));
                    Console.WriteLine("");
                }
                else
                {
                    Console.WriteLine("   ID     : {0}", seg.SGIdentifier.ToString());
                    Console.WriteLine("   Size   : {0}", seg.SGSize.ToString());
                    Console.WriteLine("   Content:");
                    Console.WriteLine(System.Text.Encoding.UTF8.GetString(seg.SGContent));
                    Console.WriteLine("   Seq    :");
                    Console.WriteLine(System.Text.Encoding.UTF8.GetString(seg.SGSeq));
                    Console.WriteLine("   Type   : {0}", TypeString(seg.SGSeqType));
                    Console.WriteLine("");
                }
            }

            // セグメント#2を削除
            int num = 0;
            foreach (SGSegment seg in SGsegs)
            {
                if (seg.SGIdentifier == 2) break;
                num++;
            }
            SGsegs.Remove(SGsegs[num]);

            string dnaFileDirectory = Path.GetDirectoryName(dnaFilePath);
            string dnaFileName = Path.GetFileNameWithoutExtension(dnaFilePath);
            string newFileName = dnaFileDirectory + "\\" + dnaFileName + "_" + ".dna";
            SaveSGFiles(SGsegs, newFileName);

            Console.WriteLine("   Saved {0}", newFileName);
            Console.WriteLine("   Hit any keys to quit...");
            Console.ReadKey();
        }
    }
}