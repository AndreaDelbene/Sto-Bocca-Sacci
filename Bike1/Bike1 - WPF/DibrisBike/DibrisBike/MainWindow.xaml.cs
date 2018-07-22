﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Win32;
using System.Data;

namespace DibrisBike
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static private SqlConnection conn;

        static private readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();
        static private readonly AutoResetEvent _signal = new AutoResetEvent(false);

        static private readonly ConcurrentQueue<object> _queueLC1 = new ConcurrentQueue<object>();
        static private readonly ConcurrentQueue<object> _queueLC2 = new ConcurrentQueue<object>();
        static private readonly ConcurrentQueue<object> _queueLC3 = new ConcurrentQueue<object>();
        static private readonly AutoResetEvent _signalLC1 = new AutoResetEvent(false);
        static private readonly AutoResetEvent _signalLC2 = new AutoResetEvent(false);
        static private readonly AutoResetEvent _signalLC3 = new AutoResetEvent(false);

        static private readonly ConcurrentQueue<object> _queueSald = new ConcurrentQueue<object>();
        static private readonly AutoResetEvent _signalSald = new AutoResetEvent(false);

        static private readonly ConcurrentQueue<int> _queueForno = new ConcurrentQueue<int>();
        static private readonly AutoResetEvent _signalForno = new AutoResetEvent(false);

        static private readonly ConcurrentQueue<int> _queueToPaint = new ConcurrentQueue<int>();
        static private readonly AutoResetEvent _signalToPaint = new AutoResetEvent(false);


        static private readonly ConcurrentQueue<object> _queuePast = new ConcurrentQueue<object>();
        static private readonly AutoResetEvent _signalPast = new AutoResetEvent(false);
        static private readonly ConcurrentQueue<object> _queueMetal = new ConcurrentQueue<object>();
        static private readonly AutoResetEvent _signalMetal = new AutoResetEvent(false);


        static private readonly ConcurrentQueue<int> _queueEssic = new ConcurrentQueue<int>();
        static private readonly AutoResetEvent _signalEssic = new AutoResetEvent(false);

        public MainWindow()
        {
            InitializeComponent();

            SqlConnection con = new SqlConnection();

            //LAPTOP - DT8KB2TQ;
            con.ConnectionString =
            "Server=SIMONE-PC\\SQLEXPRESS;" +
            "Database=stodb;" +
            "Integrated Security=True;" +
            "MultipleActiveResultSets=true;";

            conn = con;
            conn.Open();
               

            Thread t1 = new Thread(new ThreadStart(getMPSCaller));
            Thread t2 = new Thread(new ThreadStart(routingMagazzinoCaller));
            Thread t3 = new Thread(new ThreadStart(printStatoOrdini));
            Thread t4 = new Thread(new ThreadStart(accumuloSaldCaller));
            Thread t5 = new Thread(new ThreadStart(saldCaller));
            Thread t6 = new Thread(new ThreadStart(furnaceCaller));
            Thread t7 = new Thread(new ThreadStart(accumuloPaintCaller));
            Thread t8 = new Thread(new ThreadStart(paintCaller));

            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            t5.Start();
            t6.Start();
            t7.Start();
            t8.Start();
        }

        static void getMPSCaller()
        {
            MPS mps = new MPS();
            mps.getMPS(conn, _queue, _signal);
        }

        public void printStatoOrdini()
        {
            while (true)
            {
                this.Dispatcher.Invoke(() =>
                {
                    String query = "SELECT * FROM dbo.statoordini";
                    SqlCommand comm = new SqlCommand(query, conn);
                    if (conn != null && conn.State == ConnectionState.Closed)
                    {
                        conn.Open();
                    }
                    else
                    {
                        while(conn.State == ConnectionState.Connecting)
                        {
                            // wait
                        }
                        comm.ExecuteNonQuery();

                        SqlDataAdapter adapter = new SqlDataAdapter(comm);
                        DataTable table = new DataTable();
                        if (table != null)
                        {
                            adapter.Fill(table);
                            statoOrdiniGrid.ItemsSource = table.DefaultView;
                            adapter.Update(table);
                        }
                    }
                });
                Thread.Sleep(2000);
            }
        }

        static void routingMagazzinoCaller()
        {
            Routing rm = new Routing();
            rm.routingMagazzino(conn, _queue, _signal, _queueLC1, _queueLC2, _queueLC3, _signalLC1, _signalLC2, _signalLC3);
        }

        private void MPSChooser_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "Excel File|*.xlsx";
            fileDialog.DefaultExt = ".xlsx";
            Nullable<bool> dialogOk = fileDialog.ShowDialog();

            String MPSFilePath = string.Empty;
            if (dialogOk == true)
            {
                MPSFilePath = fileDialog.FileName;
                MPSPathLabel.Content = MPSFilePath;
                Thread thread = new Thread(new ThreadStart(() => {
                    getMPS(MPSFilePath);
                }));
                thread.Start();
            }
        }

        private void getMPS(string mpsFilePath)
        {
            MPS mps = new MPS();
            mps.getMPSFromFile(mpsFilePath, conn);
            updateLabel(MPSPathLabel, "MPS caricato con successo");
        }

        private void RMChooser_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "Excel File|*.xlsx";
            fileDialog.DefaultExt = ".xlsx";
            Nullable<bool> dialogOk = fileDialog.ShowDialog();

            String rawMaterialFilePath = string.Empty;
            if (dialogOk == true)
            {
                rawMaterialFilePath = fileDialog.FileName;
                RMPathLabel.Content = rawMaterialFilePath;
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    getRawMaterial(rawMaterialFilePath);
                }));
                thread.Start();
            }
        }

        private void getRawMaterial(String path)
        {
                RawMaterial rawMaterial = new RawMaterial(conn);
                rawMaterial.getRawFromFile(path);
                updateLabel(RMPathLabel, "Raw Material caricato con successo");
        }

        private void updateLabel(Label label, string message)
        {
            Action action = () => label.Content = message;
            Dispatcher.Invoke(action);
        }

        private void updateMPSLabel(string message)
        {
            Action action = () => MPSPathLabel.Content = message;
            Dispatcher.Invoke(action);
        }

        static void accumuloSaldCaller()
        {
            AccumuloSald aS = new AccumuloSald();
            aS.setAccumuloSald1(conn, _queueLC1, _signalLC1, _queueSald, _signalSald);
            aS.setAccumuloSald2(conn, _queueLC2, _signalLC2, _queueSald, _signalSald);
            aS.setAccumuloSald3(conn, _queueLC3, _signalLC3, _queueSald, _signalSald);
        }

        static void saldCaller()
        {
            Saldatura sald = new Saldatura();
            sald.startSaldatura(conn, _queueSald, _signalSald, _queueForno, _signalForno);
        }

        static void furnaceCaller()
        {
            Furnace fur = new Furnace();
            fur.startCooking(conn, _queueForno, _signalForno, _queueToPaint, _signalToPaint);
        }

        static void accumuloPaintCaller()
        {
            AccumuloPaint ap = new AccumuloPaint();
           ap.setAccumuloPaint(conn, _queueToPaint, _signalToPaint, _queuePast, _queueMetal, _signalPast, _signalMetal);
        }

        static void paintCaller()
        {
            Paint paint = new Paint();
            paint.startPaintingPast(conn, _queuePast, _signalPast, _queueEssic, _signalEssic);
            paint.startPaintingMetal(conn, _queueMetal, _signalMetal, _queueEssic, _signalEssic);
        }
        /*void ProducerThread()
        {
            while (ShouldRun)
            {
                Item item = GetNextItem();
                _queue.Enqueue(item);
                _signal.Set();
            }

        }

        void ConsumerThread()
        {
            while (ShouldRun)
            {
                _signal.WaitOne();

                Item item = null;
                while (_queue.TryDequeue(out item))
                {
                    // do stuff
                }
            }
        }*/
    }
}
