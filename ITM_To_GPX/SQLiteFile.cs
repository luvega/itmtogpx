using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DanielLibrary.GPXWriter.Main;
using DanielLibrary.GPXWriter.Utils;
using SQLiteWrapper;
using System.Collections;

namespace ITM_To_GPX
{
    namespace Utils
    {
        class SQLiteFile
        {
            private string _filename;
            private SQLiteBase _sqlitebase;

            public SQLiteFile(string filename)
            {
                _filename = Path.GetFullPath(filename);
            }

            public string filename
            {
                get
                {
                    return _filename;
                }
            }

            public void SQLiteToGPX(GPX gpxFile)
            {
                //----------------------------------------------------------------------------------------------------------------
                //Umwandlung
                //----------------------------------------------------------------------------------------------------------------                

                //Datenbank öffnen
                _sqlitebase = new SQLiteBase(_filename);

                //Waypoint Infos holen
                ArrayList WPInformation = new ArrayList();
                _ReadWaypointInformation(WPInformation);

                //Waypoints holen
                _GetWP(WPInformation, gpxFile);

                //Tracks einlesen
                ArrayList TrackInformation = new ArrayList();
                 _ReadTrackInformation(TrackInformation);

                //Tracks holen
                 _GetTracks(TrackInformation, gpxFile);

                //Datenbank zuletzt schließen
                _sqlitebase.CloseDatabase();

                //----------------------------------------------------------------------------------------------------------------
                //Umwandlung ENDE
                //----------------------------------------------------------------------------------------------------------------           
            }
            private void _ReadTrackInformation(ArrayList TrackArray)
            {
                //Abfrage an die Datenbank stellen
               System.Data.DataTable TmpTable =  _sqlitebase.ExecuteQuery("SELECT * FROM \"Line\"");

                //Anzahl der Datensätze aus Line abrufen
               int Counter = TmpTable.Rows.Count;
              
                //Die einzelnen Datensätze auslesen und speichern
                for (int i = 0; i < Counter; i++)
               {
                    //Die Datenreihe abrufen
                   System.Data.DataRow TmpRow = TmpTable.Rows[i];
                   
                    //Schauen, ob es sich um einen Track handelt
                   if ((int)TmpRow[1] == 0)
                   {
                       //Ein Speicherobjekt erstellen
                       SQLiteTracks TmpTrack = new SQLiteTracks();

                       //Name auslesen
                       TmpTrack.TrackName = (TmpRow[2]).ToString();

                       // ID auslesen
                       TmpTrack.ID = (int)(TmpRow[0]);

                       //FirstWP auslesen
                       TmpTrack.FirstWP = (int)TmpRow[9];

                       //LastWP auslesen
                       TmpTrack.LastWP = (int)TmpRow[10];

                       //Zur ArrayList hinzufügen
                       TrackArray.Add(TmpTrack);
                   }

               }

            }
            private void _GetTracks(ArrayList TrackInformation, GPX gpxfile)
            {
                //Die passende SQLite Tabelle lasen
                System.Data.DataTable datatableWP = _sqlitebase.ExecuteQuery("SELECT * FROM \"WP\"");
                System.Data.DataTable datatableGPS= _sqlitebase.ExecuteQuery("SELECT * FROM \"GPSLog\"");

                //Für jeden Track ausführen:
                foreach (SQLiteTracks sqlitetrack in TrackInformation)
                {
                    //Tracksegment erstellen
                    TrackSegment tracksegment = new TrackSegment();

                    //Variablen erstellen
                    double latitude,longitude,elevation;
                    DateTime gpxdatetime;

                    //Alle Punkte in der DB ablaufen
                    for (int i = sqlitetrack.FirstWP; i < sqlitetrack.LastWP; i++)
                    { 
                        //Punktdaten holen
                        latitude = (float) (datatableWP.Rows[i][9]);
                        longitude = (float) datatableWP.Rows[i][8];
                        elevation = (float) datatableWP.Rows[i][10];
                        gpxdatetime = DateTime.FromFileTime((((long)(int)datatableGPS.Rows[i][2]) << 32)+(int)datatableGPS.Rows[i][3]);                       

                        //Wenn Sommerzeit war, dann ziehe eine Stunde ab
                        TimeZone localZone = TimeZone.CurrentTimeZone;
                        if(localZone.IsDaylightSavingTime(gpxdatetime))
                        {
                            gpxdatetime = gpxdatetime.AddHours(-1);
                        }

                        //Zu UTC konvertieren
                        gpxdatetime = gpxdatetime.ToUniversalTime();

                        //Punkt erstellen
                        DanielLibrary.GPXWriter.Utils.Point pointtmp = new Point(latitude,longitude,elevation,gpxdatetime);

                        //Punkt zum Segment hinzufügen
                        tracksegment.AddPoint(pointtmp);
                    }

                    //Track erstellen
                    Track track = new Track();

                    //Track, Segment und GPX verbinden
                    track.AddSegment(tracksegment);
                    gpxfile.AddTrack(track);
                }
            }
            private void _ReadWaypointInformation(ArrayList WaypointArray)
            {
                 //Abfrage an die Datenbank stellen
               System.Data.DataTable TmpTable =  _sqlitebase.ExecuteQuery("SELECT * FROM \"VP\"");

                //Anzahl der Datensätze aus Line abrufen
               int Counter = TmpTable.Rows.Count;
              
                //Die einzelnen Datensätze auslesen und speichern
               for (int i = 0; i < Counter; i++)
               {
                   //Die Datenreihe abrufen
                   System.Data.DataRow TmpRow = TmpTable.Rows[i];

                   //Schauen, ob es sich um einen Track handelt
                   if ((int)TmpRow[1] == 0)
                   {
                       //Ein Speicherobjekt erstellen
                       SQLiteWP TmpWP = new SQLiteWP();

                       // ID auslesen
                       TmpWP.ID = (int)TmpRow[0];

                       // WPID auslesen
                       TmpWP.WPID = (int)TmpRow[6];

                       //Zur ArrayList hinzufügen
                       WaypointArray.Add(TmpWP);
                   }
               }
            }
            private void _GetWP(ArrayList WaypointInformation, GPX gpxfile)
            {
                //Die passende SQLite Tabelle lasen
                System.Data.DataTable datatableWP = _sqlitebase.ExecuteQuery("SELECT * FROM \"WP\"");
                System.Data.DataTable datatableGPS = _sqlitebase.ExecuteQuery("SELECT * FROM \"GPSLog\"");

                //Variablen erstellen
                double latitude, longitude, elevation;
                DateTime gpxdatetime;

                //Für jeden Waypoint ausführen:
                foreach (SQLiteWP sqliteWP in WaypointInformation)
                {
                    //Punktdaten holen
                    latitude = (float)(datatableWP.Rows[sqliteWP.WPID-1][9]);
                    longitude = (float)datatableWP.Rows[sqliteWP.WPID-1][8];
                    elevation = (float)datatableWP.Rows[sqliteWP.WPID-1][10];
                    gpxdatetime = DateTime.FromFileTime((((long)(int)datatableGPS.Rows[sqliteWP.WPID-1][2]) << 32) + (int)datatableGPS.Rows[sqliteWP.WPID-1][3]);
 
                    //Wenn Sommerzeit war, dann ziehe eine Stunde ab
                    TimeZone localZone = TimeZone.CurrentTimeZone;
                    if(localZone.IsDaylightSavingTime(gpxdatetime))
                    {
                        gpxdatetime = gpxdatetime.AddHours(-1);
                    }

                    //Zu UTC konvertieren
                    gpxdatetime = gpxdatetime.ToUniversalTime();

                    //Punkt erstellen
                    DanielLibrary.GPXWriter.Utils.Point pointtmp = new Point(latitude, longitude, elevation, gpxdatetime);

                    //Punkt zum Segment hinzufügen
                    gpxfile.AddPoint(pointtmp); 
                }
            }
        }

        public struct SQLiteTracks
        {
            public int ID;
            public string TrackName;
            public int FirstWP;
            public int LastWP;
        }
        public struct SQLiteWP
        {
            public int ID;
            public int WPID;
        }
    }

   
}
