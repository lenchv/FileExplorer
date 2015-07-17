using System;
using System.Runtime.InteropServices;
namespace BOOT
{
    public interface IBOOT 
    {
        uint VolID { get; }
        BOOT.BOOT_BPB BootBPB { get; }

        ulong beginRoot();
    }

    public class BOOT
    {
        //36 байт для всех одинаково
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BOOT_BPB
        {
                                            //Ofs   Size    Label
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] jmpBOOT;          //  0   3        Ассемблерська команда переходу до завантажувального коду
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] oemName;          //  3   8      Ім'я OEM в кодуванні ASCII

            public ushort bytePerSect;       //  11  2	    Кількість байтів в секторі
            public byte sectPerClust;       //  13  1	    Кількість  секторів у кластері
            public ushort reservedSectCount;//  14  2	    Розмір зарезервованої області у секторах
            public byte numFAT;             //  16  1   	Кількість копій FAT  
            public ushort RootEntCnt;       //  17  2       число 32х байтных элементов(дескрипторов) в корневой дииректории FAT12/16
            public ushort totSect16;        //  19  2       FAT12/16 секторов на диске
            public byte media;              //  21  1       то чем заполняется при форматировании
            public ushort fatSize16;        //  22  2       FAT12/16 количество секторов в регионе фат
            public ushort sectPerTrack;     //  24  2       секторов на дорожке
            public ushort numHead;          //  26  2       число головок
            public uint hideSec;            //  28  4       количество скрытых секторов перед началом разделов
            public uint totSec32;           //  32  4       общее число секторов на диске

            public string OEMName {
                get
                {
                    string oem = "";
                    for (int i = 0; i < oemName.Length; i++ )
                    {
                        oem += (char)oemName[i];
                    }
                    return oem;
                }
            }

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BOOT_FAT16 : IBOOT
        {                               //Ofs   Size    Label
            public BOOT_BPB beginBoot;
            public byte devType;        //36    1   	Тип(номер) пристрою
            public byte reserved1;      //37	1       
            public byte bootSeg;        //38    1	    розширена сигнатура 0x29 ASCII код “)”
            public uint volID;          //39    4	    серійний номер тома(диска)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public byte[] labelVolume;  //43    11  	мітка  тома
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] fileSysType;  //54    8   	Мітка типу файлової системи (“FAT12”, “FAT16”)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 448)]
            public byte[] reserved3;    // new byte[447];
            public ushort signature;    //510   2	сигнатура 0хАА55

            public string LabelVolume
            {
                get
                {
                    string label = "";
                    for (int i = 0; i < labelVolume.Length; i++)
                    {
                        label += (char)labelVolume[i];
                    }
                    return label;
                }
            }

            public string FileSysType
            {
                get
                {
                    string fs = "";
                    for (int i = 0; i < labelVolume.Length; i++)
                    {
                        fs += (char)fileSysType[i];
                    }
                    return fs;
                }
            }

            public uint VolID
            {
                get 
                {
                    return volID;
                }
            }

            public BOOT.BOOT_BPB BootBPB 
            {
                get
                {
                    return beginBoot;
                } 
            }

            public ulong beginRoot() 
            { 
                    /*
                     * beginFAT=begin_disk+BOOT.reservsect
                        size_obl_FAT=k_copy_FAT*size_FAT
                            де,
                            beginFAT		- початок області FAT (абсолютна адреса)
                            size_obl_FAT		- розмір області FAT
                            begin_disk		- початок логічного диску (абсолютна адреса)
                            BOOT.reservsect	- розмір резервної області
                            k_copy_FAT		- кількість копій FAT
                            size_FAT		- розмір однієї копії FAT

                    beginROOT=begin_disk+BOOT.reservsect+k_copy_FAT*size_FAT
                    size_root = k_elem_root*32/size_sekt
                        size_root		- розмір  кореневого каталогу
                        k_elem_root		- кількість дескрипторів у кореневому каталозі
                        size_sekt		- розмір одного сектора
                    */
                ulong beginRoot = ((ulong)beginBoot.reservedSectCount + (ulong)beginBoot.numFAT * (ulong)beginBoot.fatSize16) * (ulong)beginBoot.bytePerSect;
                return beginRoot;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BOOT_FAT32:IBOOT
        {
            //Ofs   Size    Label
            public BOOT_BPB beginBoot; //
            public uint fatSize32;     //36    4   	    Розмір однієї копії FAT (у секторах)
            public ushort extFlags;    //40    2       Поля только до FAT32 номер активной таблицы FAT
            public ushort fsVer;       //42    2   	    В старшем байте номер версии ОС в младшем номер подверсии
            public uint rootClust;     //44    4       Номер первого кластера корневой директории
            public ushort fsInfo;      //48    2       Номер сектора, в котором находится структура FSINFO
            public ushort bkBootSec;   //50    2       копия boot сектора
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] reserved2;   //52    12      резерв
            public byte dpvNum;        //64	   1       номер драйвера(номер диска BIOS int13)
            public byte reserved3;     //65    1	    
            public byte bootSeg;       //66    1       сигнатура
            public uint volID;         //67    4       серийный номер диска
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public byte[] labelVolume; //71    11  	метка тома
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] fileSysType; //82    8       "FAT32"
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 420)]
            public byte[] reserved4;   // = new byte[419];//...	 
            public ushort signature;   //510-511	Сигнатура 0хАА55

            public uint VolID
            {
                get
                {
                    return volID;
                }
            }

            public BOOT.BOOT_BPB BootBPB
            {
                get
                {
                    return beginBoot;
                }
            }

            public ulong beginRoot() 
            {
                /*
                 * beginFAT=begin_disk+BOOT.reservsect
                    size_obl_FAT=k_copy_FAT*size_FAT
                        де,
                        beginFAT		- початок області FAT (абсолютна адреса)
                        size_obl_FAT		- розмір області FAT
                        begin_disk		- початок логічного диску (абсолютна адреса)
                        BOOT.reservsect	- розмір резервної області
                        k_copy_FAT		- кількість копій FAT
                        size_FAT		- розмір однієї копії FAT

                Для FAT32
                    beginROOT=begin_disk+BOOT.reservsect+k_copy_FAT*size_FAT+
                    +(N_klast-2) * size_klast
                    N_klast	- перший номер кластера кореневого каталогу
                    size_klast	- кількість секторів у кластері

                */
                ulong beginRoot = ((ulong)beginBoot.reservedSectCount + (ulong)beginBoot.numFAT * (ulong)fatSize32 + 
                                                   (ulong)(rootClust-2)*(ulong)beginBoot.sectPerClust) * (ulong)beginBoot.bytePerSect;
                return beginRoot;
             }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BOOT_NTFS : IBOOT
        {
            //Ofs   Size    Label
            public BOOT_BPB beginBoot;
            public uint physDisk;           //36    4       вроде как зарезервировано   
            public ulong numberOfSector;    //40    8       число секторов в разделе
            public ulong mftClust;          //48    8       стартовый кластер MFT
            public ulong mftMirr;           //56    8       вторичная MFT
            public sbyte clusterPerMftRecord;//64    1       размер одной записи MFT (если < 0, то размер вычисляется как 2^abs(clusterPerMftRecord))
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] reserved1;          //65    3       
            public byte sizeIndexRecord;    //68    1       размер индексной записи
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] reserved2;          //69    3  
            public ulong volID;             //72    8       серийный номер тома
            public uint crc;         //80    4
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 426)]
            public byte[] reserved3;
            public ushort signature;     //510-511	Сигнатура 0хАА55

            public uint VolID
            {
                get
                {
                    return (uint)volID;
                }
            }

            public BOOT.BOOT_BPB BootBPB
            {
                get
                {
                    return beginBoot;
                }
            }

            public uint SizeRecordMFT
            {
                get
                {
                    if (clusterPerMftRecord > 0)
                    {
                        return (uint)clusterPerMftRecord;
                    }
                    else
                    {
                        uint size = (uint)(1 << Math.Abs(clusterPerMftRecord ));
                        return size;
                    }
                }
            }

            public ulong beginRoot() 
            {
                /*
                    beginRoot = beginLogicDisk + (secPerClust * firstMFT+32) * 512 
                */
                ulong beginRoot = ((ulong)beginBoot.sectPerClust * mftClust + 32) * (ulong)beginBoot.bytePerSect;
                return beginRoot;
            }
        }

        static public IBOOT fillBOOT(byte[] buffer, Type type)
        {
            IntPtr pnt = Marshal.AllocHGlobal(512);
            IBOOT boot;
            try
            {
                Marshal.Copy(buffer, 0, pnt, buffer.Length);
                boot = (IBOOT)Marshal.PtrToStructure(pnt, type);
            }
            finally
            {
                Marshal.FreeHGlobal(pnt);
            }
            return boot;
        }

        /*static void Main(string[] args)
        {
            byte[] buffer = new byte[512];
            for (byte i = 0; i < 255; i++)
                buffer[i] = 1;
            for (int i = 256; i < 512; i++)
                buffer[i] = 1;
            // Initialize unmanged memory to hold the array.
            BOOT_FAT16 boot;
            boot = (BOOT_FAT16)fillBOOT(buffer, typeof(BOOT_FAT16));
            
            Console.ReadKey();
        }*/
    }
}