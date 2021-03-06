﻿using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Threading;

namespace DibrisBike
{
    class Welding
    {
        public Welding()
        {
        }

        public void startSaldatura(SqlConnection conn, ConcurrentQueue<object> _queueSald, AutoResetEvent _signalSald, ConcurrentQueue<int> _queueForno, AutoResetEvent _signalForno)
        {
            while(true)
            {
                _signalSald.WaitOne();
                object codiceBarreTemp,idLottoTemp;
                string[] codiceBarre;
                int idLotto;
                while(_queueSald.TryDequeue(out codiceBarreTemp))
                {
                    _queueSald.TryDequeue(out idLottoTemp);
                    codiceBarre = (string[])codiceBarreTemp;
                    idLotto = (int)idLottoTemp;

                    string query = "UPDATE dbo.statoordini SET stato = @stato WHERE idLotto = @idLotto";
                    SqlCommand comm = new SqlCommand(query, conn);
                    comm.Parameters.Clear();
                    comm.Parameters.AddWithValue("@stato", "storing for welding");
                    comm.Parameters.AddWithValue("@idLotto", idLotto);

                    comm.ExecuteNonQuery();

                    //heating up the welder
                    query = "INSERT INTO dbo.processirt (type, date, value) VALUES (@type, @date, @value)";
                    comm = new SqlCommand(query, conn);
                    comm.Parameters.Clear();
                    comm.Parameters.AddWithValue("@type", "S001_P1");
                    comm.Parameters.AddWithValue("@date", DateTime.Now);
                    comm.Parameters.AddWithValue("@value", 0);

                    comm.ExecuteNonQuery();

                    //transponting the tubes from the storage to the welder (saldatrice)
                    Thread.Sleep(1000);

                    //going for the welder then
                    //Console.WriteLine("WELDING");
                    //creating a new row into the table that contains frames that are being welded/cooked/painted/dried
                    query = "INSERT INTO dbo.saldessdp (startTimeSald,stato) VALUES (@startTimeSald, @stato)";

                    comm = new SqlCommand(query, conn);
                    //state is "welming"
                    comm.Parameters.Clear();
                    comm.Parameters.AddWithValue("@startTimeSald", DateTime.Now);
                    comm.Parameters.AddWithValue("@stato", "welding");
                   
                    comm.ExecuteNonQuery();
                    //once we have a number for the frame, we get it
                    query = "SELECT TOP 1 idTelaio FROM dbo.saldessdp ORDER BY idTelaio DESC";

                    comm = new SqlCommand(query, conn);
                    comm.Parameters.Clear();
                    SqlDataReader reader;

                    reader = comm.ExecuteReader();
                    reader.Read();
                    int idTelaio = (int)reader["idTelaio"];
                    reader.Close();
                    //and then we come back to update the storage infos.
                    for (int i = 0; i < codiceBarre.Length; i++)
                    {
                        query = "UPDATE dbo.accumulosaldaturadp SET idTelaio = @idTelaio WHERE codiceTubo = @codiceTubo";

                        comm = new SqlCommand(query, conn);
                        comm.Parameters.Clear();
                        comm.Parameters.AddWithValue("@idTelaio", idTelaio);
                        comm.Parameters.AddWithValue("@codiceTubo", codiceBarre[i]);

                        comm.ExecuteNonQuery();
                    }

                    query = "UPDATE dbo.statoordini SET stato = @stato WHERE idLotto = @idLotto";
                    comm = new SqlCommand(query, conn);
                    comm.Parameters.Clear();
                    comm.Parameters.AddWithValue("@stato", "welding");
                    comm.Parameters.AddWithValue("@idLotto", idLotto);
                    
                    comm.ExecuteNonQuery();

                    //setting data into the queue for the Furnace
                    _queueForno.Enqueue(idTelaio);
                    _queueForno.Enqueue(idLotto);
                    //and we signal it.
                    _signalForno.Set();
                }
            }
        }
    }
}
