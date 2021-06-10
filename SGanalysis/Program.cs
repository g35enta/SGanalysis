using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace SGanalysis
{
    class Program
    {
        public interface ISGSegment
        {
            byte SGIdentifier { get; }
            int SGSize { get; }
            byte[] SGContent { get; }
            byte SGSeqType { get; }
            byte[] SGSeq { get; }
        }

        /// <summary>
        /// SnapGeneファイルセグメントを1件保持する読み取り専用クラス
        /// </summary>
        public class SGSegment : ISGSegment
        {
            public Byte SGIdentifier { get; }
            public Int32 SGSize { get; }
            public Byte[] SGContent { get; }
            public Byte SGSeqType { get; }
            public Byte[] SGSeq { get; }

            public SGSegment() { }
            public SGSegment(byte identifier, int size, byte[] content, byte type, byte[] seq)
            {
                SGIdentifier = identifier;
                SGSize = size;
                SGContent = content;
                SGSeqType = type;
                SGSeq = seq;
            }
            public SGSegment(byte identifier, int size, byte[] content)
            {
                SGIdentifier = identifier;
                SGSize = size;
                SGContent = content;
            }
        }

        /// <summary>
        /// SnapGeneファイルを扱うための処理をサポートするインターフェース
        /// </summary>
        public interface ISGFile
        {
            // セグメントを列挙して返す
            IEnumerable<SGSegment> Read();
            // セグメントを受け取ってSGFileを作成する
            bool Save(IEnumerable<SGSegment> segs, string filePath);
        }

        /// <summary>
        /// SnapGeneファイルを保持するクラス
        /// </summary>
        public class SGFile : ISGFile
        {
            // SnapGeneファイルのフルパス
            private string filePath;
            public SGFile() { }
            public SGFile(string filePath)
            {
                this.filePath = filePath;
            }
            public IEnumerable<SGSegment> Read()
            {
                // 返す用のリスト
                List<SGSegment> output = new List<SGSegment>();

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    int fileSize = (int)fs.Length;
                    while (br.BaseStream.Position != fileSize)
                    {
                        SGSegment seg = new SGSegment();
                        // id
                        byte id = br.ReadByte();
                        // size
                        byte[] sizeByteArray = br.ReadBytes(4);
                        Array.Reverse(sizeByteArray);
                        int size = BitConverter.ToInt32(sizeByteArray, 0);
                        // content
                        byte[] content = br.ReadBytes(size);

                        if (id == 0)
                        {
                            // contentの内部情報（type, seq）
                            byte type = content[0];
                            byte[] seq = new byte[content.Length - 1];
                            Array.Copy(content, 1, seq, 0, content.Length - 1);
                            //SGSegmentをインスタンス化
                            seg = new SGSegment(id, size, content, type, seq);
                        }
                        else
                        {
                            seg = new SGSegment(id, size, content);
                        }

                        // List<SGSegment>に追加
                        output.Add(seg);
                    }
                }

                return output;
            }

            public bool Save(IEnumerable<SGSegment> segs, string filePath)
            {
                if (segs == null || filePath == "") return false;

                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    foreach (SGSegment seg in segs)
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
        }

        /// <summary>
        /// ファイルパスから具象クラスをインスタンス化するFactoryクラス。
        /// filePathで判別して処理を分岐可能。
        /// </summary>
        public static class ISGFileFactory
        {
            public static ISGFile Create(string filePath)
            {
                return new SGFile(filePath);
            }
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

        /// <summary>
        /// CSVファイル作成用
        /// </summary>
        internal class SGSegTable : ClassMap<SGSegment>
        {
            private SGSegTable()
            {
                Map(c => c.SGIdentifier).Index(0);
                Map(c => c.SGSize).Index(1);
                Map(c => c.SGContent).Index(2);
                Map(c => c.SGSeqType).Index(3);
                Map(c => c.SGSeq).Index(4);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine("  SnapGeneファイル解析ツール");
            Console.WriteLine("    Copyright (c) 2021 Genta Ito");
            Console.WriteLine("    Version 2.0");
            Console.WriteLine("======================================================");
            Console.WriteLine("  ダンプファイルをテキストおよびCSVファイルで出力します。");
            Console.WriteLine("");

            // ファイルパスの取得
            string[] filePath = Environment.GetCommandLineArgs();
            string dnaFilePath = filePath[1];

            // ダンプ用ファイルパスの生成
            string dnaFileDirectory = Path.GetDirectoryName(dnaFilePath);
            string dnaFileName = Path.GetFileNameWithoutExtension(dnaFilePath);
            string dumpCsvFilePath = dnaFileDirectory + "\\" + dnaFileName + "_dump" + ".csv";
            string dumpTxtFilePath = dnaFileDirectory + "\\" + dnaFileName + "_dump" + ".txt";

            // 読み込み
            ISGFile file = ISGFileFactory.Create(dnaFilePath);
            IEnumerable<SGSegment> SGsegs = file.Read();

            // ダンプCSVファイル出力
            using (var sw = new StreamWriter(dumpCsvFilePath))
            using (var csv = new CsvWriter(sw, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(SGsegs);
            }
            Console.WriteLine("   Saved {0}", dumpCsvFilePath);
            Console.WriteLine("");

            // ダンプTXTファイル出力
            using (var sw = new StreamWriter(dumpTxtFilePath, false))
            {
                foreach (SGSegment seg in SGsegs)
                {
                    sw.WriteLine("ID: " + seg.SGIdentifier.ToString());
                    sw.WriteLine("Size: " + seg.SGSize.ToString());
                    sw.WriteLine("Content:");
                    sw.WriteLine(System.Text.Encoding.UTF8.GetString(seg.SGContent));
                    if (seg.SGSeqType != 0) sw.WriteLine("Sequence Type: " + seg.SGSeqType.ToString());
                    if (seg.SGSeq != null) sw.WriteLine(System.Text.Encoding.UTF8.GetString(seg.SGSeq));
                    sw.WriteLine("");
                }
            }
            Console.WriteLine("   Saved {0}", dumpTxtFilePath);
            Console.WriteLine("");

            // 仮実装
            // 別のファイル名（末尾に"_"を追加）で保存
            string newFilePath = dnaFileDirectory + "\\" + dnaFileName + "_" + ".dna";
            file.Save(SGsegs, newFilePath);

            Console.WriteLine("   Saved {0}", newFilePath);
            Console.WriteLine("");
            Console.WriteLine("   Hit any keys to quit...");
            Console.ReadKey();
        }
    }
}