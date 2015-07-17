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
using System.IO;
using MasterBootRecord;


namespace FileExplorer
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        List<LogicalDisk> diskList = new List<LogicalDisk>(); 
        public MainPage()
        {
            this.Initialized += MainPage_Initialized;
            InitializeComponent();
        }

        void MainPage_Initialized(object sender, EventArgs e)
        {

            FileStream fDrive0 = null;
            BufferedStream bDrive0 = null;
            MBR mbr = null;
            DescriptorFile handle = null;
            try
            {
                int i = 0;
                while (true)
                {
                    handle = new DescriptorFile(@"\\.\PHYSICALDRIVE" + i);
                    if (handle.FileHandle == null)
                        return;

                    fDrive0 = new FileStream(handle.FileHandle, FileAccess.ReadWrite);
                    bDrive0 = new BufferedStream(fDrive0, 512);
                    mbr = new MBR(bDrive0, 0);
                    MBR.ofsAddr = 0;
                    foreach (LogicalDisk l in LogicalDisk.getLogicalDisk(mbr, bDrive0))
                    {
                        diskList.Add(l);
                    }

                    bDrive0.Dispose();
                    handle.FileHandle.Close();
                    i++;
                }
            }
            catch (FileNotFoundException ex)
            {
                fDrive0.Dispose();
                fDrive0.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            listViewDisk.DataContext = diskList;
            
        }

        private void Button_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LogicalDisk ld = (LogicalDisk)((sender as Button).Content as StackPanel).DataContext;
            try
            {
                DescriptorFile dDisk = new DescriptorFile(String.Format("\\\\.\\{0}", ld.Letter));
                if (dDisk.FileHandle != null)
                {
                    DirectoryPage dp = new DirectoryPage();
                    dp.init(ld, new Directory(ld.BootSector, dDisk, 2, ld.Letter + "\\", true));
                    dp.Title = ld.Letter;
                    NavigationService.Navigate(dp);
                }
                else
                {
                    MessageBox.Show("Ошибка открытия дескриптора логического диска!");
                }
                dDisk.FileHandle.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
