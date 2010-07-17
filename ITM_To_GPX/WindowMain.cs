using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using ITM_To_GPX.Utils;
using DanielLibrary.GPXWriter.Main;

namespace ITM_To_GPX
{
    public partial class WindowMain : Form
    {
        public WindowMain()
        {
            InitializeComponent();            
        }

        private void ButtonOpen_Click(object sender, EventArgs e)
        {
            // OpenFileDialog Instanz erstellen und einen "Öffnen...." Dialog anzeigen, 
            // indem mehrere Dateien ausgewählt werden können, die anschließend in die
            // ListBoxOpened eingefügt werden
            OpenFileDialog TmpFileDialog = new OpenFileDialog();
            TmpFileDialog.Multiselect = true;
            TmpFileDialog.Filter = "GPSPhototagger File (*.itm)|*.itm";

            if (TmpFileDialog.ShowDialog() == DialogResult.OK)
            {

                ListBoxOpened.BeginUpdate();

                foreach (string TmpPfad in TmpFileDialog.FileNames)
                {
                    ListBoxOpened.Items.Add(TmpPfad);
                }

                ListBoxOpened.EndUpdate();

            }

 
        }

        private void ButtonConvert_Click(object sender, EventArgs e)
        {
            //Lege einen Buffer für später an
            byte[] Buffer = new Byte[4096];

            //Frag den Ort ab, an dem die GPX gespeichert werden sollen
            FolderBrowserDialog TmpFolderBrowser = new FolderBrowserDialog();
            TmpFolderBrowser.ShowDialog();

            foreach (object TmpObject in ListBoxOpened.Items)
            {   
                //ZipFile initalisieren zum Entpacken
                ZipFile TmpFile = null;

                try
                {
                    //Öffne die Datei aus dem Kasten
                    TmpFile = new ZipFile(TmpObject.ToString());

                    //Gehe Alle Dateien durch
                    foreach (ZipEntry TmpEntry in TmpFile)
                    {
                        //Bis eine den namen ituser.poi hat
                        if (TmpEntry.IsFile && TmpEntry.Name == "ituser.poi\0")
                        {
                            //Wenn das so ist, dann hole dir einen Stream auf dieses File                           
                            Stream TmpFileStream = TmpFile.GetInputStream(TmpEntry);

                            //erstelle den neuen Dateinamen, indem du den vorhin ausgewählten Pfad nimmst
                            string TmpFullPath = Path.Combine(TmpFolderBrowser.SelectedPath, "ituser.poi");
                            
                            //und erstelle die Datei
                            using (FileStream TmpOutput = File.Create(TmpFullPath))
                            {     
                                //anschließend kopiere sie aus der zip dorthin und schließe sie
                                StreamUtils.Copy(TmpFileStream, TmpOutput, Buffer);
                                TmpOutput.Close();

                                //Jetzt erstelle ein GPX und SQL Objekt
                                SQLiteFile sqlitefile = new SQLiteFile(TmpFullPath);
                                GPX gpxfile = new GPX(Path.Combine(TmpFolderBrowser.SelectedPath,Path.ChangeExtension(Path.GetFileName(TmpObject.ToString()),".gpx")));

                                //Gpx File erstellen
                                sqlitefile.SQLiteToGPX(gpxfile);

                                //und schreiben
                                gpxfile.WriteGPX();

                                //TmpOutput löschen
                                File.Delete(TmpFullPath);
                            }


                        }

                    }
                }
                finally
                {
                    if (TmpFile != null)
                    {
                        TmpFile.IsStreamOwner = true;
                        TmpFile.Close();
                    }
                }
            }

        }

        private void ButtonDelete_Click(object sender, EventArgs e)
        {
            int TmpSelected = ListBoxOpened.SelectedIndex;
            if (TmpSelected >= 0)
            {
                // Markiertes Objekt in ListBoxOpened löschen
                ListBoxOpened.Items.Remove(ListBoxOpened.Items[TmpSelected]);

                // Wenn sich darunter noch ein Element befindet, dann wird dieses ausgewählt
                // Ansonsten ist kein Element ausgewählt
                if (ListBoxOpened.Items.Count > TmpSelected)
                {
                    ListBoxOpened.SelectedIndex = TmpSelected;
                }
            }

        }
    }
}
