using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MasterBootRecord;
using BOOT;
using LogicalDisk;
using System.IO;
using System.Management;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
           
            PartitionTable pt = new PartitionTable();
            FileStream fDrive0 = null;
            BufferedStream bDrive0 = null;
            MBR mbr = null;
            DescriptorFile handle = null;

            try
            {
                handle = new DescriptorFile(@"\\.\PHYSICALDRIVE0");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (handle.FileHandle == null)
                return;

            fDrive0 = new FileStream(handle.FileHandle, FileAccess.ReadWrite);
            bDrive0 = new BufferedStream(fDrive0, 512);
            mbr = new MBR(bDrive0, 0);

            MBR.showMBR(mbr, " ");

            Console.WriteLine("------------------");
            List<LogicalDisk.LogicalDisk> l = LogicalDisk.LogicalDisk.getLogicalDisk(mbr, bDrive0);
            l.Sort(new LogicalDisk.LogicalDiskCompare());
            /*foreach (LogicalDisk.LogicalDisk ld in l)
            {
                if (ld.BootSector != null)
                    Console.WriteLine("{0}  {1}  {2:X}", ld.Letter, ld.FileSystem, ld.BootSector.beginRoot() + ld.BeginDisk);
            }*/
            
            LogicalDisk.LogicalDisk disk1 = l[2];
            DescriptorFile lDisk1 = null;
            try {
                lDisk1 = new DescriptorFile(String.Format("\\\\.\\{0}", disk1.Letter));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (lDisk1.FileHandle == null)
                return;
            
            LogicalDisk.Directory dir1 = new LogicalDisk.Directory(disk1.BootSector, lDisk1, 2, disk1.Letter + "\\", true);
            ulong numClustDir2 = 0;
            foreach (LogicalDisk.File file in dir1.Files)
            {
                Console.WriteLine("{0}\t{1}", file.Name, file.Attributes);
                if (file.Name.CompareTo("WINDOWS") == 0)
                {
                    numClustDir2 = (ulong)file.NumberCluster; 
                }
            }
            
           if (numClustDir2 != 0)
            {
                Console.WriteLine("Новая папка (2)");
                LogicalDisk.Directory dir2 = new LogicalDisk.Directory(disk1.BootSector, lDisk1, numClustDir2, disk1.Letter + "\\Новая папка (2)", false);

                foreach (LogicalDisk.File file in dir2.Files)
                {
                    Console.WriteLine("{0}\t{1}", file.Name, file.Attributes);
                }
           }
            
            Console.ReadKey();
            bDrive0.Close();
            fDrive0.Close();
            handle.FileHandle.Close();
        }
    }
}
