using System;
using System.Collections.Generic;
using System.Management;
using System.IO;
using BOOT;
using MasterBootRecord;
using System.Runtime.InteropServices;

namespace FileExplorer
{
    public class LogicalDisk
    {
        private PartitionTable partitionInfo;
        private IBOOT bootSector;
        ulong beginDisk;
        private string letter;
        private string fileSystem;

        public LogicalDisk() {}
        public LogicalDisk(PartitionTable pt, IBOOT boot, ulong beginAddress) 
        {
            this.partitionInfo = pt;
            this.bootSector = boot;
            this.beginDisk = beginAddress;
            if (boot == null)
            {
                this.letter = "*";
            }
            else
            {
                this.letter = getLetterDisk(boot.VolID);
            }

            if (boot is BOOT.BOOT.BOOT_FAT16)
            {
                if (validationFAT(boot) < 4085)
                {
                    this.fileSystem = "FAT12";
                }
                else if (validationFAT(boot) < 65525)
                {
                    this.fileSystem = "FAT16";
                }
                else
                {
                    throw new Exception("Тип раздела в таблице MBR не верен");
                }
            }
            else if (boot is BOOT.BOOT.BOOT_FAT32)
            {
                if (validationFAT(boot) >= 65525)
                {
                    this.fileSystem = "FAT32";
                }
                else
                {
                    throw new Exception("Тип раздела в таблице MBR не верен");
                }
            }
            else if (boot is BOOT.BOOT.BOOT_NTFS)
            {
                this.fileSystem = "NTFS";
            }
            else
            {
                this.fileSystem = "UNKNOWN";
            }
        }

        public static string getLetterDisk(uint serialNumber)
        {
            string letter = "";
            string query = String.Format("SELECT * FROM Win32_LogicalDisk WHERE VolumeSerialNumber = \"{0:X}\"", serialNumber);
            ManagementObjectSearcher disk = new ManagementObjectSearcher(query);
            foreach (ManagementObject d in disk.Get())
            {
                letter =  d.GetPropertyValue("Name").ToString();
            }
            return letter;
        }

        public long GetFreeSpace
        {
            get
            {
                try
                {
                    DriveInfo d = new DriveInfo(letter + "\\");
                    return d.TotalFreeSpace/(1024*1024);
                }
                catch (Exception) { }
                return 0;
            }
        }

        public int GetBuzySpace
        {
            get
            {
                return 150 - ((int)(((double)GetFreeSpace / ((double)SizeDisk)) * 150));
            }
        }

        public string Letter
        {
            get
            {
                return this.letter;
            }
        }

        public string FileSystem
        {
            get
            {
                return this.fileSystem;
            }
        }

        public ulong SizeDisk
        {
            get
            {
                return ((ulong)partitionInfo.CountSectors * 512)/(1024*1024); 
            }
        }

        public PartitionTable PartitionInfo
        {
            get
            {
                return partitionInfo;
            }
        }

        public BOOT.IBOOT BootSector
        {
            get
            {
                return bootSector;
            }
        }
        
        public ulong BeginDisk 
        {
            get 
            {
                return beginDisk;
            }
        }

        private uint validationFAT(IBOOT boot)
        {
            BOOT.BOOT.BOOT_FAT32 boot32;
            uint rootDirSec = (((uint)boot.BootBPB.RootEntCnt * 32) + ((uint)boot.BootBPB.bytePerSect - 1))/(uint)boot.BootBPB.bytePerSect;
            uint fatSize;//размер таблицы FAT
            uint totSect;//число секторов на диске
            uint dataSec;//секторов в области данных
            uint countOfClusters; //число кластеров
            if (boot.BootBPB.fatSize16 != 0)
            {
                fatSize = (uint)boot.BootBPB.fatSize16;
            }
            else
            {
                boot32 = (BOOT.BOOT.BOOT_FAT32)boot;
                fatSize = boot32.fatSize32;
            }

            if (boot.BootBPB.totSect16 != 0)
            {
                totSect = (uint)boot.BootBPB.totSect16;
            }
            else
            {
                totSect = boot.BootBPB.totSec32;
            }
            dataSec = totSect - (boot.BootBPB.reservedSectCount + (boot.BootBPB.numFAT * fatSize) + rootDirSec);
            countOfClusters = dataSec / boot.BootBPB.sectPerClust;
            return countOfClusters;
        }

        static public List<LogicalDisk> getLogicalDisk(MBR mbr, BufferedStream bufStream)
        {
            List<LogicalDisk> logicalDisks = new List<LogicalDisk>(3);
            byte[] buffer = new byte[512];
            IBOOT strucBOOT;
            LogicalDisk logicalDisk;
            for (int i = 0; i < 4; i++)
            {
                if (mbr[i] == null) break;
                if (mbr[i] is MBR)
                {
                    List<LogicalDisk> tmp = getLogicalDisk((MBR)mbr[i], bufStream);
                    foreach (LogicalDisk ld in tmp)
                    {
                        logicalDisks.Add(ld);
                    }
                }
                else
                {
                    PartitionTable pt = (PartitionTable)mbr[i];
                    bufStream.Seek(pt.getLba() + mbr.getLba(), SeekOrigin.Begin);
                    bufStream.Read(buffer, 0, buffer.Length);
                    switch (pt.PartitionType)
                    {
                        case 0x01:
                        case 0x04:
                        case 0x06:
                        case 0x0E:
                        case 0x11:
                        case 0x14:
                        case 0x16:
                        case 0x1E:
                            //FAT16/12
                            strucBOOT = (BOOT.BOOT.BOOT_FAT16)BOOT.BOOT.fillBOOT(buffer, typeof(BOOT.BOOT.BOOT_FAT16));
                            break;
                        case 0x0B:
                        case 0x0C:
                        case 0x1B:
                        case 0x1C:
                            //FAT32
                            strucBOOT = (BOOT.BOOT.BOOT_FAT32)BOOT.BOOT.fillBOOT(buffer, typeof(BOOT.BOOT.BOOT_FAT32));
                            break;
                        case 0x07:
                            //NTFS
                            strucBOOT = (BOOT.BOOT.BOOT_NTFS)BOOT.BOOT.fillBOOT(buffer, typeof(BOOT.BOOT.BOOT_NTFS));
                            break;
                        default:
                            strucBOOT = null;
                            break;
                    }
                    logicalDisk = new LogicalDisk(pt, strucBOOT, (ulong)pt.getLba() + (ulong)mbr.getLba());
                    logicalDisks.Add(logicalDisk);
                }
            }
            return logicalDisks;
        }
    }

    public class LogicalDiskCompare : IComparer<LogicalDisk>
    {
        public int Compare(LogicalDisk disk1, LogicalDisk disk2)
        {
            return disk1.Letter.CompareTo(disk2.Letter);
        }
    }

    public class Directory
    {

        private List<File> files;
        private string path;
        private ulong numberOfCluster;
        private bool root16;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SFN : IFN
        {
                                        // ofs  size    content
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] name;           //0     8       имя файла 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] ext;            //8     3       расширение
            public byte attribute;      //11    1       аттрибуты файла (ReadOnly (x01)| 
                                        //                               HIDDEN (x02)| 
                                        //                               SYSTEM (x04)| 
                                        //                               VOLUME_ID (x08)| 
                                        //                               DIRECTORY (x10)| 
                                        //                               ARCHIVE (x20) ) sum = LONG NAME 
            public byte reserved;       //12    1       зарезервировано, всегда = 0
            public byte crtTimeTenth;   //13    1       милисекунды текущего времени создания, содержит 10 доли миллисекунд (1 - 199)
            public ushort crtTime;      //14    2       время создания
            public ushort crtDate;      //16    2       дата создания
            public ushort lstAccDate;   //18    2       дата последнего обращения
            public ushort fstClustHi;   //20    2       старшая часть № 1го кластера файла
            public ushort wrtTime;      //22    2       время изменения файла
            public ushort wrtDate;      //24    2       дата изменения файла
            public ushort fstClusLow;   //26    2       младшая часть № 1го кластера файла
            public uint fileSize;       //28    4       размер файла

            public string Name
            {
                get
                {
                    string str = "";
                    for (int i = 0; i < name.Length; i++)
                    {
                        str += (char)name[i];
                    }
                    return str;
                }
            }

            public string Extension
            {
                get
                {
                    string str = "";
                    for (int i = 0; i < ext.Length; i++)
                    {
                        str += (char)ext[i];
                    }
                    return str;
                }
            }

            public Attribute Attribute
            {
                get
                {
                    return (Attribute)attribute;
                }
            }

            public uint FileSize
            {
                get
                {
                    return fileSize;
                }
            }

            public uint NumberCluster
            {
                get
                {
                    uint numClust = ((uint)fstClustHi << 16) + fstClusLow;
                    return numClust;
                }
            }

            public string CreateTime
            {
                get
                {
                    /**
                     * | 5|6 бит = минуты|5бит = секунды в 2х секундных интервалах|
                     */
                    int sec = ((int)crtTime & 0x001F) * 2;
                    int min = ((int)crtTime >> 5) & 0x003F;
                    int hour = (int)crtTime >> 11;
                    return String.Format("{0}:{1}:{2}", hour, min, sec);
                }
            }

            public string CreateDate
            {
                get
                {
                    /**
                     * | 7 бит = год + 1980|4 бит = месяц|5бит = день|
                     */
                    int day = (int)crtDate & 0x001F;
                    int month = ((int)crtDate >> 5) & 0x000F;
                    int year = ((int)crtDate >> 9) + 1980;
                    return String.Format("{0}.{1}.{2}", day, month, year);
                }
            }

            public string ModTime
            {
                get
                {
                    int sec = ((int)wrtTime & 0x001F) * 2;
                    int min = ((int)wrtTime >> 5) & 0x003F;
                    int hour = (int)wrtTime >> 11;
                    return String.Format("{0}:{1}:{2}", hour, min, sec);
                }
            }

            public string ModDate
            {
                get
                {
                    int day = (int)wrtDate & 0x001F;
                    int month = ((int)wrtDate >> 5) & 0x000F;
                    int year = ((int)wrtDate >> 9) + 1980;
                    return String.Format("{0}.{1}.{2}", day, month, year);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct LFN : IFN
        {                           // ofs  size    content
            public byte ord;        // 0    1       порядковый номер записи в длинном имени
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public ushort[] name1;  // 1    10      1ая часть имени (1 - 5) 
            public byte attribute;  // 11   1       аттрибуты
            public byte type;       // 12   1       если 0, запись является компонентом LFN
            public byte chkSum;     // 13   1       контрольная сумма 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public ushort[] name2;  // 14   12      вторая часть имени (6 - 11)
            public ushort fstClusLow;//26   2       для LFN всегда 0
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public ushort[] name3;   //28    4       последние 2 символа имени (12-13)

            public string Name
            {
                get
                {
                    string str = "";
                    for (int i = 0; i < name1.Length; i++)
                    {
                        if (name1[i] == 0)
                            return str;
                        str += (char)name1[i];
                    }
                    for (int i = 0; i < name2.Length; i++)
                    {
                        if (name2[i] == 0)
                            return str;
                        str += (char)name2[i];
                    }
                    for (int i = 0; i < name3.Length; i++)
                    {
                        if (name3[i] == 0)
                            return str;
                        str += (char)name3[i];
                    }
                    return str;
                }
            }
        }

        public Directory() {}
       
        public Directory(IBOOT boot, DescriptorFile handle, ulong numDirClust, string path, bool rootFat16)
        {
            int bufferSize = 0;
            this.path = path;
            this.numberOfCluster = numDirClust;
            this.root16 = rootFat16;
            FileStream fDisk1 = new FileStream(handle.FileHandle, FileAccess.Read);
            List<ulong> chainClusters = null;
            
            if (rootFat16 && (boot is BOOT.BOOT.BOOT_FAT16))
            {
                bufferSize = boot.BootBPB.RootEntCnt * 32;
                chainClusters = new List<ulong>(1);
                chainClusters.Add(boot.beginRoot());
                this.files = getFiles(bufferSize, fDisk1, chainClusters);
            } else if(boot is BOOT.BOOT.BOOT_NTFS) {
                this.files = getFilesNTFS(path);
            }
            else 
            {
                bufferSize = (int)(boot.BootBPB.sectPerClust * boot.BootBPB.bytePerSect);
                chainClusters = getChainClusters(boot, numDirClust, fDisk1);
                this.files = getFiles(bufferSize, fDisk1, chainClusters);
            }
            //Console.WriteLine("BufSize = {0}\tSizeClust = {1}", bufferSize, boot.BootBPB.sectPerClust);
            fDisk1.Dispose();
            fDisk1.Close();
        }

        public List<File> Files
        {
            get
            {
                return files;
            }
        }

        public string Path
        {
            get
            {
                return path;
            }
        }

        public ulong NumberOfCluster
        {
            get
            {
                return numberOfCluster;
            }
        }

        public bool isRoot
        {
            get
            {
                return root16;
            }
        }

        /**************NTFS*****************************
         * Получает список файлов для NTFS
         */
        public List<File> getFilesNTFS(string path)
        {
            List<File> files = new List<File>(5);
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(path);
            foreach (System.IO.DirectoryInfo dir in dirInfo.EnumerateDirectories())
            {
                files.Add(new File(dir.Name,
                                        "",
                                        (Attribute)dir.Attributes,
                                            0,
                                            0,
                                            dir.CreationTime.ToShortTimeString(),
                                            dir.CreationTime.ToShortDateString(),
                                            dir.LastWriteTime.ToShortTimeString(),
                                            dir.LastWriteTime.ToShortDateString(),
                                            path));
                                            
            }
            foreach (System.IO.FileInfo file in dirInfo.EnumerateFiles())
            {

                files.Add(new File(file.Name,
                                        file.Extension,
                                        (Attribute)file.Attributes,
                                            (uint)file.Length,
                                            0,
                                            file.CreationTime.ToShortTimeString(),
                                            file.CreationTime.ToShortDateString(),
                                            file.LastWriteTime.ToShortTimeString(),
                                            file.LastWriteTime.ToShortDateString(),
                                            path));
                                            
            }

            return files;
        }

        /**
         * получает список файлов в текущей директории 
         */
        public List<File> getFiles(int bufferSize, FileStream fs, List<ulong> chainClusters)
        {
             
            byte[] buffer = new byte[bufferSize];   //буффер на весь кластер
            byte[] bufFN = new byte[32];            //буффер на одну запись
            List<File> files = new List<File>();
            SFN sfn;                                //запись короткого имени
            LFN lfn;                                //запись длинного имени
            string name = "";                       //переменная для формирования имени
            int n = 0;                              //индекс части имени в массиве частей длинного имени
            string[] longName = null;                      //массив частей длинного имени
            foreach(ulong clust in chainClusters)
            {
                fs.Seek((long)clust, SeekOrigin.Begin);
                fs.Read(buffer, 0, bufferSize);
                for (int i = 0; i < buffer.Length; i += 32)
                {
                    if (buffer[i] != 0xe5)  //если удален
                    {
                        if (buffer[i] == 0) //если конец записей
                        {
                            break;
                        }
                        else
                        {
                            if (buffer[i + 11] == 0x0F) //если длинное имя
                            {
                                if(name == "") 
                                {
                                    longName = new string[buffer[i] - 0x40];
                                    n = 0;
                                }
                                
                                do
                                {
                                    Array.Copy(buffer, i, bufFN, 0, 32);
                                    lfn = (LFN)fillFN(bufFN, typeof(LFN));
                                    longName[n] += lfn.Name;
                                    i += 32;
                                    n++;
                                } while (i < buffer.Length && buffer[i + 11] == 0x0F);
                                Array.Reverse(longName);
                                name = String.Join("", longName);

                                /**если кластер закончился, а записи еще остались,
                                 * то читаем следующий кластер, при этом переменная имени не обнуляется
                                */
                                if (i >= buffer.Length)
                                {
                                    Array.Reverse(longName);
                                    break;
                                }                                
                            }
                            Array.Copy(buffer, i, bufFN, 0, 32);
                            sfn = (SFN)fillFN(bufFN, typeof(SFN));
                            if (name == "")//если есть длинное имя, то короткое имя не нужно сохранять
                            {
                                name = sfn.Name.Trim();
                                if (!sfn.Attribute.HasFlag(Attribute.DIRECTORY)) 
                                    name += "." + sfn.Extension.Trim();
                            }
                            if (name != "." && name != "..")
                                files.Add(new File(name, sfn, path));
                            name = "";//поле имени чиститься только после создания объекта File
                        }
                    }
                }
            }
            
            return files;
        }
        /**
         * получает цепочку кластеров, начиная с заданного кластера startNumClust
         */
        public List<ulong> getChainClusters(IBOOT boot, ulong startNumClust, FileStream f)
        {
            List<ulong> chainClusters = new List<ulong>(2);

            ulong addrClust = getClustAddress(startNumClust, boot);//адрес кластера в области данных
            chainClusters.Add(addrClust);

            long addrClustInFat = (long)getSectorClustInFat(startNumClust, boot);//адресс сектора кластера в FAT
            long ofs = (long)getOffsetClustInFAT(startNumClust, boot);//смещение до класетра в FAT
            byte[] buffer = new byte[boot.BootBPB.bytePerSect];
            /***********************************************************************/
            //Console.WriteLine("{0} - {1} - {2}", addrClust, addrClustInFat, ofs);
            /*************************************************************************/
            f.Seek(addrClustInFat, SeekOrigin.Begin);
            f.Read(buffer, 0, boot.BootBPB.bytePerSect);
            MemoryStream ms = new MemoryStream(buffer);
            BinaryReader br = new BinaryReader(ms);
            ms.Seek(ofs, SeekOrigin.Begin);
            if (boot is BOOT.BOOT.BOOT_FAT16)
            {
                ushort numClust = br.ReadUInt16();
                if (numClust >= 0xFFF8)
                {
                    return chainClusters;
                }
                else
                {
                    List<ulong> tmp = new List<ulong>(2);
                    tmp = getChainClusters(boot, (ulong)numClust, f);
                    foreach (ulong el in tmp)
                    {
                        chainClusters.Add(el);
                    }
                }
            }
            else if (boot is BOOT.BOOT.BOOT_FAT32)
            {
                uint numClust = br.ReadUInt32();
                if (numClust >= 0x0FFFFFF8)
                {
                    return chainClusters;
                }
                else
                {
                    List<ulong> tmp = new List<ulong>(2);
                    tmp = getChainClusters(boot, (ulong)numClust, f);
                    foreach (ulong el in tmp)
                    {
                        chainClusters.Add(el);
                    }
                }
            }
            ms.Dispose();
            br.Close();
            ms.Close();
            return chainClusters;
        }
        /**
         * получает адрес кластера в области данных
         */
        public ulong getClustAddress(ulong N, IBOOT boot)
        {
            ulong ofs = (ulong)(boot.BootBPB.RootEntCnt * 32);
            return (((N - 2) * (ulong)boot.BootBPB.sectPerClust) * (ulong)boot.BootBPB.bytePerSect) + boot.beginRoot() + ofs;
        }
        /**
         * получает адрес сектора в байтаъх в таблице FAT
         */
        public ulong getSectorClustInFat(ulong N, IBOOT boot)
        {
            ulong nFATSector;
            ulong FAToffset;
            if (boot is BOOT.BOOT.BOOT_FAT16)
                FAToffset = N * 2;
            else
                FAToffset = N * 4;
            nFATSector = (ulong)boot.BootBPB.reservedSectCount + (FAToffset / (ulong)boot.BootBPB.bytePerSect);
            return nFATSector * (ulong)boot.BootBPB.bytePerSect;
        }
        /**
         * получает смещение на кластер в таблице FAT
         */
        public ulong getOffsetClustInFAT(ulong N, IBOOT boot)
        { 
            ulong FAToffset;
            if (boot is BOOT.BOOT.BOOT_FAT16)
                FAToffset = N * 2;
            else
                FAToffset = N * 4;
            return FAToffset % (ulong)boot.BootBPB.bytePerSect;
        }
        /**
         * Заполняет одну из структур файловой записи
         */
        public IFN fillFN(byte[] buffer, Type type)
        {
            IntPtr pnt = Marshal.AllocHGlobal(32);
            IFN fn;
            try
            {
                Marshal.Copy(buffer, 0, pnt, 32);
                fn = (IFN)Marshal.PtrToStructure(pnt, type);
            }
            finally
            {
                Marshal.FreeHGlobal(pnt);
            }
            return fn;
        }
    }

    public class FileCompare : IComparer<File>
    {
        public int Compare(File file1, File file2)
        {
            if (file1 == null || file2 == null)
            {
                return 0; 
            } else 
            if ((file1.Attributes.HasFlag(Attribute.DIRECTORY)) && (file2.Attributes.HasFlag(Attribute.DIRECTORY)))
            {
                return file1.Name.CompareTo(file2.Name);
            }
            if (file1.Attributes.HasFlag(Attribute.DIRECTORY))
            {
                return -1;
            }
            else if (file2.Attributes.HasFlag(Attribute.DIRECTORY)) 
            {
                return -1;
            } else 
            {
                return +1;
            }
        }
    }

    [Flags]
    public enum Attribute : byte
    {
        NONE     = 0x00,
        READONLY = 0x01, 
        HIDDEN   = 0x02, 
        SYSTEM   = 0x04,
        VOLUME_ID= 0x08, 
        DIRECTORY= 0x10, 
        ARCHIVE  = 0x20
    }
    public class File
    {
        private string name;
        private string extension;
        private Attribute attributes;
        private uint fileSize;
        private uint numberCluster;
        private string createTime;
        private string createDate;
        private string modTime;
        private string modDate;
        private string fullName;
        public bool isSelected { get; set; }

        public File(string name, 
                    string extension,
                    Attribute attribute,
                    uint fileSize,
                    uint numberCluster,
                    string createTime,
                    string createDate,
                    string modTime,
                    string modDate,
                    string fullName)
        {
            this.name = name;
            this.extension = extension;
            this.attributes = attribute;
            this.fileSize = fileSize;
            this.numberCluster = numberCluster;
            this.createTime = createTime;
            this.createDate = createDate;
            this.modTime = modTime;
            this.modDate = modDate;
            this.fullName = fullName;

            isSelected = false;
        }

        public File(File file) : this(file.Name,
                                      file.Extension,
                                      file.Attributes,
                                      file.FileSize,
                                      file.NumberCluster,
                                      file.CreateTime,
                                      file.CreateDate,
                                      file.ModTime,
                                      file.ModDate,
                                      file.Path) { isSelected = false; }

        public File(string name, Directory.SFN sfn, string fullName)
        {
            this.name = name;
            this.extension = sfn.Extension;
            this.attributes = sfn.Attribute;
            this.fileSize = sfn.FileSize;
            this.numberCluster = sfn.NumberCluster;
            this.createTime = sfn.CreateTime;
            this.createDate = sfn.CreateDate;
            this.modTime = sfn.ModTime;
            this.modDate = sfn.ModDate;
            this.fullName = fullName;

            isSelected = false;
        }

        public string Path
        {
            get
            {
                return fullName;
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }
        public string Extension
        {
            get
            {
                return extension;
            }
        } 
        public Attribute Attributes
        {
            get
            {
                return attributes;
            }
        }
        public uint FileSize
        {
            get
            {
                return fileSize;
            }
        }
        public uint NumberCluster
        {
            get
            {
                return numberCluster;
            }
        }
        public string CreateTime
        {
            get
            {
                return createTime;
            }
        }
        public string CreateDate
        {
            get
            {
                return createDate;
            }
        }
        public string ModTime
        {
            get
            {
                return modTime;
            }
        }
        public string ModDate
        {
            get
            {
                return modDate;
            }
        }
    }

    public interface IFN
    {
        
    }
}
