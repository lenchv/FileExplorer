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
using System.Windows.Shapes;
using MasterBootRecord;

namespace FileExplorer
{
    /// <summary>
    /// Логика взаимодействия для WindowIndivTask.xaml
    /// </summary>
    public partial class WindowIndivTask : Window
    {
        LogicalDisk ld;
        public Directory dir {get; set;}
        public WindowIndivTask(LogicalDisk ld)
        {
            this.ld = ld;
            this.Initialized += WindowIndivTask_Initialized;
            InitializeComponent();
        }

        void WindowIndivTask_Initialized(object sender, EventArgs e)
        {
            try
            {
                DescriptorFile dDisk = new DescriptorFile(String.Format("\\\\.\\{0}", ld.Letter));
                if (dDisk.FileHandle != null)
                {
                    this.dir = new Directory(ld.BootSector, dDisk, 2, ld.Letter + "\\", true);
                    TreeViewItem item = new TreeViewItem();
                    item.Header = ld.Letter;
                    foreach (File f in dir.Files)
                    {
                        if (f.Attributes.HasFlag(Attribute.DIRECTORY))
                        {
                            TreeViewItem newItem = new TreeViewItem();
                            newItem.Tag = f;
                            newItem.Header = f.Name;
                            newItem.Items.Add("*");
                            item.Items.Add(newItem);
                        }
                    }
                    treeView1.Items.Add(item);
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File file = (File)(treeView1.SelectedItem as TreeViewItem).Tag;
                DescriptorFile dDisk = new DescriptorFile(String.Format("\\\\.\\{0}", ld.Letter));
                if (dDisk.FileHandle != null)
                {
                    uint numClust = 0;
                    if (file.NumberCluster == 0)
                    {
                        numClust += 2;
                    }
                    else
                    {
                        numClust = file.NumberCluster;
                    }
                    this.dir = new Directory(ld.BootSector, dDisk, numClust, file.Path + "\\" + file.Name, false);

                }
                else
                {
                    MessageBox.Show("Ошибка открытия дескриптора логического диска!");
                }
                dDisk.FileHandle.Close();
            }
            catch (NullReferenceException ex) { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            this.DialogResult = true;
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem rootItem = (e.OriginalSource as TreeViewItem);
            if(rootItem.Header.ToString().Length != ld.Letter.Length)
            {
                try
                {
                    DescriptorFile dDisk = new DescriptorFile(String.Format("\\\\.\\{0}", ld.Letter));
                    if (dDisk.FileHandle != null)
                    {
                        File file = (File)rootItem.Tag;
                        uint numClust = 0;
                        if (file.NumberCluster == 0)
                        {
                            numClust += 2;
                        }
                        else
                        {
                            numClust = file.NumberCluster;
                        }
                        this.dir = new Directory(ld.BootSector, dDisk, numClust, file.Path + "\\" + file.Name, false);
                        rootItem.Items.RemoveAt(0);
                        foreach (File f in dir.Files)
                        {
                            if (f.Attributes.HasFlag(Attribute.DIRECTORY))
                            {
                                TreeViewItem newItem = new TreeViewItem();
                                newItem.Tag = f;
                                newItem.Header = f.Name;
                                newItem.Items.Add("*");
                                rootItem.Items.Add(newItem);
                            }
                        }
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
}
