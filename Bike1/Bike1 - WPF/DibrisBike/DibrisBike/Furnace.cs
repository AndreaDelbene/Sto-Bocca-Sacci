﻿using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace DibrisBike
{
    class Furnace
    {
        public Furnace()
        {
        }

        public void startCooking(SqlConnection conn, ConcurrentQueue<int> _queueForno, AutoResetEvent _signalForno, ConcurrentQueue<int> _queueToPaint, AutoResetEvent _signalToPaint)
        {
            while(true)
            {
                _signalForno.WaitOne();
                int idTelaio;
                int idLotto;
                while(_queueForno.TryDequeue(out idTelaio))
                {
                    _queueForno.TryDequeue(out idLotto);
                    // simulating the Welmer
                    Thread.Sleep(8000);

                    //Let's cook the frame now!
                    Console.WriteLine("FURNACE");
                    string query = "UPDATE dbo.saldessdp SET stato = @stato, endTimeSald = @endTimeSald, startTimeForno = @startTimeForno WHERE idTelaio = @idTelaio";

                    SqlCommand comm = new SqlCommand(query, conn);
                    //state is "cooking"
                    comm.Parameters.Clear();
                    comm.Parameters.AddWithValue("@stato", "cooking");
                    comm.Parameters.AddWithValue("@idTelaio", idTelaio);
                    comm.Parameters.AddWithValue("@endTimeSald", DateTime.Now);
                    comm.Parameters.AddWithValue("@startTimeForno", DateTime.Now);
                    
                    comm.ExecuteNonQuery();

                    //updating the order state table
                    query = "UPDATE dbo.statoordini SET stato = @stato WHERE idLotto = @idLotto";
                    comm = new SqlCommand(query, conn);
                    comm.Parameters.Clear();
                    comm.Parameters.AddWithValue("@stato", "cooking");
                    comm.Parameters.AddWithValue("@idLotto", idLotto);
                    
                    comm.ExecuteNonQuery();
                    //filling the queue for the Painting and signaling it
                    _queueToPaint.Enqueue(idTelaio);
                    _queueToPaint.Enqueue(idLotto);
                    _signalToPaint.Set();
                }
            }
        }
    }
}
