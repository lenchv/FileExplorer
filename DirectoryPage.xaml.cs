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
using MasterBootRecord;
using BOOT;

namespace FileExplorer
{
    /// <summary>
    /// Логика взаимодействия для DirectoryPage.xaml
    /// </summary>
    public partial class DirectoryPage : Page
    {
        LogicalDisk currentDisk;
        Directory dir;
        bool isShift, isF2;
        public DirectoryPage()
        {
            InitializeComponent();

            listView1.KeyDown += DirectoryPage_KeyDown;
            listView1.KeyUp += listView1_KeyUp;
        }

        public void init(LogicalDisk ld, Directory dir)
        {
            currentDisk = ld;
            this.dir = dir;
            lblPath.Content = dir.Path;
            listView1.DataContext = dir.Files;
        }

        void tb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            File file = (File)(sender as ListViewItem).Content;
            if (file.Attributes.HasFlag(Attribute.DIRECTORY))
            {
                try
                {
                    DescriptorFile dDisk = new DescriptorFile(String.Format("\\\\.\\{0}", currentDisk.Letter));
                    uint numClust = 0;
                    if (file.NumberCluster == 0)
                    {
                        numClust += 2;
                    }
                    else
                    {
                        numClust = file.NumberCluster;
                    }

                    Directory d = new Directory(currentDisk.BootSector, dDisk, numClust, file.Path + "\\" + file.Name, false);
                    DirectoryPage dp = new DirectoryPage();
                    dp.init(currentDisk, d);
                    dp.Title = file.Name;
                    NavigationService.Navigate(dp);
                    dDisk.FileHandle.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DescriptorFile dDisk = new DescriptorFile(String.Format("\\\\.\\{0}", currentDisk.Letter));
                dir = new Directory(currentDisk.BootSector, dDisk, dir.NumberOfCluster, dir.Path, dir.isRoot);
                listView1.DataContext = dir.Files;
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void listView1_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftShift: isShift = false;
                    break;
                case Key.F2: isF2 = false;
                    break;
            }
        }

        void DirectoryPage_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftShift: isShift = true;
                    break;
                case Key.F2: isF2 = true;
                    break;
            }
            if (isShift && isF2)
            {
                indivTask();
                isShift = false;
                isF2 = false;
            }
        }

        public void indivTask()
        {
            WindowIndivTask task = new WindowIndivTask(currentDisk);
            if (task.ShowDialog() == true)
            {
                List<File> newList = new List<File>();
                foreach (File f in listView1.Items)
                {
                    f.isSelected = false;
                    if (!f.Attributes.HasFlag(Attribute.DIRECTORY))
                    {
                        foreach (File item in task.dir.Files)
                        {
                            if (!item.Attributes.HasFlag(Attribute.DIRECTORY))
                            {
                                if (f.Name.CompareTo(item.Name) == 0)
                                {
                                    f.isSelected = true;
                                    break;
                                }
                            }
                        }
                    }
                    newList.Add(f);
                }
                listView1.DataContext = newList;
            }
        }

        private void btnIndiv_Click(object sender, RoutedEventArgs e)
        {
            indivTask();
        }
    }

    [ValueConversion(typeof(bool), typeof(Brush))]
    public class SelectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value == true)
            {
                return new SolidColorBrush(Colors.Red);
            }
            else
            {
                return new SolidColorBrush(Colors.Black);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    /**
     *В зависимости от аттрибута файла выбирается соответствующая иконка
     */
    [ValueConversion(typeof(Attribute), typeof(string))]
    public class IconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Attribute attr = (Attribute)value;
            if (attr.HasFlag(Attribute.DIRECTORY))
            {
                return "/Icon/folder.ico";
            }
            else
            {
                return "/Icon/file.ico";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
