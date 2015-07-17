using System;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace MasterBootRecord
{
    public class PartitionTable : IMasterBootRecord
    {
        private byte active;
        private byte beginHead;
        private byte beginSect;
        private byte beginTrack2;
        private byte beginTrack8;
        private byte partitionType;
        private byte endHead;
        private byte endSect;
        private byte endTrack2;
        private byte endTrack8;
        private uint lba;
        private uint countSect;

        public PartitionTable() { }
        public PartitionTable(PartitionTable pt)
        {
            active = pt.active;
            beginHead = pt.beginHead;
            beginSect = pt.beginSect;
            beginTrack2 = pt.beginTrack2;
            beginTrack8 = pt.beginTrack8;
            partitionType = pt.partitionType;
            endHead = pt.endHead;
            endSect = pt.endSect;
            endTrack2 = pt.endTrack2;
            endTrack8 = pt.endTrack8;
            lba = pt.lba;
            countSect = pt.countSect;
        }

        public byte Active
        {
            set
            {
                active = value;
            }
            get
            {
                return active;
            }
        }

        public byte BeginHead
        {
            set
            {
                beginHead = value;
            }
            get
            {
                return beginHead;
            }
        }

        public byte BeginSect
        {
            set
            {
                beginSect = (byte)(value & 0x0000003F);
                beginTrack2 = (byte)(value & 0x000000C0);
            }
            get
            {
                return beginSect;
            }
        }

        public ushort BeginTrack
        {
            set
            {
                beginTrack8 = (byte)value;
            }
            get
            {
                int tmp = beginTrack2 >> 8;
                tmp = (int)beginTrack8;
                return (ushort)tmp;
            }
        }

        public byte PartitionType
        {
            set
            {
                partitionType = value;
            }
            get
            {
                return partitionType;
            }
        }

        public byte EndHead
        {
            set
            {
                endHead = value;
            }
            get
            {
                return endHead;
            }
        }

        public byte EndSect
        {
            set
            {
                endSect = (byte)(value & 0x0000003F);
                endTrack2 = (byte)(value & 0x000000C0);
            }
            get
            {
                return endSect;
            }
        }

        public ushort EndTrack
        {
            set
            {
                endTrack8 = (byte)value;
            }
            get
            {
                int tmp = endTrack2 >> 8;
                tmp = (int)endTrack8;
                return (ushort)tmp;
            }
        }

        public uint LBA
        {
            set
            {
                lba = value;
            }
            get
            {
                return lba;
            }
        }

        public uint CountSectors
        {
            set
            {
                countSect = value;
            }
            get
            {
                return countSect;
            }
        }

        public void load(BinaryReader br)
        {
            Active = br.ReadByte();
            BeginHead = br.ReadByte();
            BeginSect = br.ReadByte();
            BeginTrack = (ushort)br.ReadByte();
            PartitionType = br.ReadByte();
            EndHead = br.ReadByte();
            EndSect = br.ReadByte();
            EndTrack = (ushort)br.ReadByte();
            LBA = br.ReadUInt32();
            CountSectors = br.ReadUInt32();
        }

        public void show(string separator)
        {
            Console.WriteLine("{0}Active {1:X}",separator, Active);
            Console.WriteLine("{0}Type {1:X}", separator, PartitionType);
            Console.WriteLine("{0}LBA {1}", separator, LBA);

            Console.WriteLine("{0}Quantity sectors {1}", separator, CountSectors);
        }

        public long getLba()
        {
            return (long)LBA * 512;
        }
    }

    public class MBR : IMasterBootRecord
    {
        private IMasterBootRecord[] partition = new IMasterBootRecord[4]; 
        static public long ofsAddr = 0;   //смещение до следующей MBR
        private long address = 0;   //адрес сектора MBR
        private BufferedStream currentDrive; //поток с которым работаем
        private byte[] buf; //буффер для чтения всего сектора с MBR
            

        public MBR(BufferedStream drive, long address)
        {
            this.address = address;
            this.buf = new byte[512];
            this.currentDrive = drive;
            drive.Seek(address, SeekOrigin.Begin); //смещаемся на сектор с MBR
            drive.Read(buf, 0, buf.Length); //читаем весь сектор в буффер

            MemoryStream memStream = new MemoryStream(buf); // записываем буффер в поток
            memStream.Seek(446, SeekOrigin.Begin);//смещаемся на таблицу разделов
            BinaryReader br = new BinaryReader(memStream);//создаем поток бинарного чтения
            //и читаем все партиции
            PartitionTable pt = new PartitionTable();
            for (int i = 0; i < 4; i++)
            {
                pt.load(br);
                this[i] = pt;
            }
            br.Dispose();
            br.Close();
            memStream.Dispose();
            memStream.Close();
        }
            
        public IMasterBootRecord this[int ind]
        {
            set
            {
                if (ind < 0 || ind > 4)
                {
                    throw new IndexOutOfRangeException("В таблице MBR может быть не больше 4х разделов");
                }
                PartitionTable pt = (PartitionTable)value;
                if (pt.PartitionType == 0)
                {
                    partition[ind] = null;
                }
                else if (pt.PartitionType == 0x05 || pt.PartitionType == 0x0F)
                {
                    if (MBR.ofsAddr == 0)
                    {
                        MBR.ofsAddr = pt.getLba();
                        partition[ind] = new MBR(currentDrive, pt.getLba());
                    }
                    else
                    {
                        partition[ind] = new MBR(currentDrive, pt.getLba() + MBR.ofsAddr);
                    }
                }
                else
                {
                    partition[ind] = new PartitionTable(pt);
                }
            }
            get
            {
                if (ind < 0 || ind > 4)
                {
                    throw new IndexOutOfRangeException("В таблице MBR может быть не больше 4х разделов");
                }
                return partition[ind];
            }
        }

        static public void showMBR(MBR mbr, string sep)
        {
            string separator = sep;
            Console.WriteLine("---------------------------------");
            PartitionTable pt = new PartitionTable();
            for (int i = 0; i < 4; i++)
            {
                    
                if (mbr[i] == null) break;
                if (mbr[i] is MBR)
                {
                    showMBR((MBR)mbr[i], separator + "-");
                }
                else
                {
                    pt = (PartitionTable)mbr[i];
                    pt.show(separator);
                }
            }

            separator.Remove(separator.Length - 1);
        }

        public long getLba()
        {
            return address;
        }
    }

    public class DescriptorFile
    {
        public const short FILE_ATTRIBUTE_NORMAL = 0x80;
        public const short INVALID_HANDLE_VALUE = -1;
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint CREATE_NEW = 1;
        public const uint CREATE_ALWAYS = 2;
        public const uint OPEN_EXISTING = 3;
        const uint FILE_SHARE_READ = 0x00000001;
        const uint FILE_SHARE_WRITE = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFile(
                                                    string lpFileName,
                                                    uint dwDesiredAccess,
                                                    uint dwShareMode,
                                                    IntPtr lpSecurityAttributes,
                                                    uint dwCreationDisposition,
                                                    uint dwFlagsAndAttributes,
                                                    IntPtr hTemplateFile
                                                );
        private SafeFileHandle handleFile = null;

        public DescriptorFile(string path) {
            Load(path);
        } 

        public void Load(string path) {

            if (path == null || path.Length == 0)
            {
                throw new ArgumentNullException("Неверный путь к файлу");
            }

            this.handleFile = CreateFile(path,
                                            GENERIC_READ,
                                            FILE_SHARE_READ | FILE_SHARE_WRITE,
                                            IntPtr.Zero,
                                            OPEN_EXISTING,
                                            0,
                                            IntPtr.Zero
                                    );
            if (this.handleFile.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        public SafeFileHandle FileHandle
        {
            get
            {
                if (this.handleFile.IsInvalid)
                {
                    return null;
                }
                else
                {
                    return this.handleFile;
                }
            }
        }
    }

    public interface IMasterBootRecord
    {
        long getLba();
    }
}

