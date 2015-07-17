using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;

using MasterBootRecord;
using BOOT;
using System.IO;

namespace FileExplorer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileList dir;
        LogicalDisk disk1;
        DescriptorFile lDisk1;
        uint backClust;
        string backPath;

        public MainWindow()
        {
            dir = (FileList)(this.FindResource("MyFiles") as ObjectDataProvider).Data;
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        void tb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            File file = (File)(sender as ListViewItem).Content;
            if (file.Attributes.HasFlag(Attribute.DIRECTORY))
            {
                try
                {
                    lDisk1 = new DescriptorFile(String.Format("\\\\.\\{0}", disk1.Letter));
                    uint numClust = 0;
                    if (file.NumberCluster == 0)
                    {
                        numClust += 2;
                    }
                    else
                    {
                        numClust = file.NumberCluster;
                    }

                    dir.Directory = new Directory(disk1.BootSector, lDisk1, numClust, file.Path+"\\"+file.Name, false);
                    backFolder.IsEnabled = true;
                    backPath = file.Path;
                    if (dir.Directory.Files.Count > 2 && dir.Directory.Files[1].Name == "..")
                    {
                        backFolder.IsEnabled = true;
                        backClust = dir.Directory.Files[1].NumberCluster;
                        if (backClust == 0) backClust = 2;
                    }
                    else
                    {
                       // backFolder.IsEnabled = false;
                    }
                    lDisk1.FileHandle.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                
            }
        }

        public void backFolderEvent(object sender, MouseButtonEventArgs e)
        {
            try
            {
                lDisk1 = new DescriptorFile(String.Format("\\\\.\\{0}", disk1.Letter));
                dir.Directory = new Directory(disk1.BootSector, lDisk1, backClust, backPath, false);

                if (dir.Directory.Files.Count > 2 && dir.Directory.Files[1].Name == "..")
                {
                    backFolder.IsEnabled = true;
                    backClust = dir.Directory.Files[1].NumberCluster;
                    if (backClust == 0) backClust = 2;
                }
                else
                {
                   // backFolder.IsEnabled = false;
                }

                backPath = dir.Directory.Path;
                lDisk1.FileHandle.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FileStream fDrive0 = null;
            BufferedStream bDrive0 = null;
            MBR mbr = null;
            DescriptorFile handle = null;

            try
            {
                handle = new DescriptorFile(@"\\.\PHYSICALDRIVE1");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (handle.FileHandle == null)
                return;

            fDrive0 = new FileStream(handle.FileHandle, FileAccess.ReadWrite);
            bDrive0 = new BufferedStream(fDrive0, 512);
            mbr = new MBR(bDrive0, 0);

            List<LogicalDisk> l = LogicalDisk.getLogicalDisk(mbr, bDrive0);
            l.Sort(new LogicalDiskCompare());
            /*foreach (LogicalDisk.LogicalDisk ld in l)
            {
                if (ld.BootSector != null)
                    Console.WriteLine("{0}  {1}  {2:X}", ld.Letter, ld.FileSystem, ld.BootSector.beginRoot() + ld.BeginDisk);
            }*/

            disk1 = l[0];
            try
            {
                lDisk1 = new DescriptorFile(String.Format("\\\\.\\{0}", disk1.Letter));
                if (lDisk1.FileHandle != null)
                {
                    dir.Directory = new Directory(disk1.BootSector, lDisk1, 2, disk1.Letter + "\\", true);
                }
                else
                {
                    MessageBox.Show("Ошибка открытия дескриптора логического диска!");
                }
                lDisk1.FileHandle.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            
            bDrive0.Close();
            fDrive0.Close();
            handle.FileHandle.Close();
        }
    }
    /**
     *Класс коллекции для вывода в TextView  
     */
    class FileList:ObservableCollection<File>
    {
        Directory directory;
        public FileList() { }
        public FileList(Directory dir)
        {
            directory = dir;
            Update();
        }
        public Directory Directory
        {
            set
            {
                directory = value;
                Update();
            }
            get
            {
                return directory;
            }
        }
        private void Update()
        {
            this.Clear();
            foreach (File file in directory.Files)
            {
                Add(file);
            }
        }
    }
    
}
