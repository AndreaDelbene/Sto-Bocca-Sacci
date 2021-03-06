﻿using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace DibrisBike
{
    class Routing
    {
        public Routing()
        {
        }

        public void routingMagazzino(SqlConnection conn, ConcurrentQueue<object> _queue, AutoResetEvent _signal, ConcurrentQueue<object> _queueLC1, ConcurrentQueue<object> _queueLC2,
            ConcurrentQueue<object> _queueLC3, AutoResetEvent _signalLC1, AutoResetEvent _signalLC2, AutoResetEvent _signalLC3, AutoResetEvent _signalError, AutoResetEvent _signalErrorRM)
        {
            while (true)
            {
                //waiting until some data comes from the queue
                _signal.WaitOne();
                //getting it then
                object idLottoTemp, quantitaTubiTemp, lineaTemp, quantitaOrdineTemp;
                int[] idLotto, quantitaTubi, quantitaOrdine;
                string[] linea;
                //'til the queue is full, i get data from it
                while(_queue.TryDequeue(out idLottoTemp))
                {
                    _queue.TryDequeue(out quantitaTubiTemp);
                    _queue.TryDequeue(out lineaTemp);
                    _queue.TryDequeue(out quantitaOrdineTemp);

                    idLotto = (int[])idLottoTemp;
                    quantitaTubi = (int[])quantitaTubiTemp;
                    linea = (string[])lineaTemp;
                    quantitaOrdine = (int[])quantitaOrdineTemp;

                    string[] tipoTelaio = new string[idLotto.Length];

                    for(int i = 1; i < 4; i++)
                    {
                        //heating the laser cuts
                        string query = "INSERT INTO dbo.processirt (type, date, value) VALUES (@type, @date, @value)";
                        SqlCommand comm = new SqlCommand(query, conn);
                        comm.Parameters.Clear();
                        comm.Parameters.AddWithValue("@type", "LC00" + i + "_P1");
                        comm.Parameters.AddWithValue("@date", DateTime.Now);
                        comm.Parameters.AddWithValue("@value", 0);

                        comm.ExecuteNonQuery();
                    }


                    for (int i = 0; i < idLotto.Length; i++)
                    {
                        //we need to cycle on how many bikes the customer asked for
                        for (int j = 0; j < quantitaOrdine[i]; j++)
                        {
                            //and checking, for each request, whenever I have still tubes in the storage
                            string query = "SELECT TOP (@quantita) * FROM dbo.magazzinomateriali";
                            SqlCommand comm = new SqlCommand(query, conn);
                            comm.Parameters.Clear();
                            comm.Parameters.AddWithValue("@quantita", quantitaTubi[i]);

                            SqlDataAdapter adapter = new SqlDataAdapter(comm);

                            comm.ExecuteNonQuery();

                            DataTable table = new DataTable();
                            adapter.Fill(table);
                            string[] codiceBarre;

                            codiceBarre = (from DataRow r in table.Rows select (string)r["codiceBarre"]).ToArray();

                            //If i have it, i proceed in updating the 'routing' table
                            if (table.Rows.Count == quantitaTubi[i])
                            {
                                //deleting every tube I get from the storage.
                                for (int k = 0; k < quantitaTubi[i]; k++)
                                {
                                    //after i created the realation frame - tubes
                                    query = "INSERT INTO dbo.pezzotubo (idLotto, idPezzo, codiceTubo) VALUES (@idLotto, @idPezzo, @codiceTubo)";
                                    comm = new SqlCommand(query, conn);
                                    comm.Parameters.Clear();
                                    comm.Parameters.AddWithValue("@idLotto", idLotto[i]);
                                    comm.Parameters.AddWithValue("@idPezzo", idLotto[i] + " - " + (j + 1));
                                    comm.Parameters.AddWithValue("@codiceTubo", codiceBarre[k]);
                                    
                                    comm.ExecuteNonQuery();

                                    query = "DELETE FROM dbo.magazzinomateriali WHERE codiceBarre = @codiceBarre";
                                    comm = new SqlCommand(query, conn);
                                    comm.Parameters.Clear();
                                    comm.Parameters.AddWithValue("@codiceBarre", codiceBarre[k]);
                                    
                                    comm.ExecuteNonQuery();
                                }

                                //selecting the frame (telaio) type
                                query = "SELECT tipoTelaio FROM dbo.mps WHERE id = @idLotto";
                                comm = new SqlCommand(query, conn);
                                SqlDataReader reader;
                                comm.Parameters.Clear();
                                comm.Parameters.AddWithValue("@idLotto", idLotto[i]);

                                reader = comm.ExecuteReader();
                                reader.Read();

                                tipoTelaio[i] = (string)reader["tipoTelaio"];

                                reader.Close();

                                //getting a random number to select in which Laser Cut send the set of tubes.
                                //alternatively it's possible to do a control on queue's dimensions and pick the lowest one.
                                Random r = new Random();

                                int rInt = r.Next(1, 4);
                                bool flag = false;
                                int idPercorso = 0;

                                //preparing the insertion into the routing table
                                //Laser Cut step
                                query = "INSERT INTO dbo.routing (idLotto,idPezzo,step,durata,durataSetUp,opMacchina) VALUES (@idLotto,@idPezzo,@step,@durata,@durataSetUp,@opMacchina)";

                                comm = new SqlCommand(query, conn);
                                comm.Parameters.Clear();
                                comm.Parameters.AddWithValue("@idLotto", idLotto[i]);
                                comm.Parameters.AddWithValue("@idPezzo", idLotto[i] + " - " + (j + 1));
                                comm.Parameters.AddWithValue("@step", 1);
                                comm.Parameters.AddWithValue("@durata", 9);
                                comm.Parameters.AddWithValue("@durataSetUp", 1);
                                
                                switch (tipoTelaio[i])
                                {
                                    case "graziella":
                                        comm.Parameters.AddWithValue("@opMacchina", rInt);
                                        flag = true;
                                        idPercorso = rInt;
                                        break;

                                    case "corsa":
                                        comm.Parameters.AddWithValue("@opMacchina", rInt);
                                        flag = true;
                                        idPercorso = rInt;
                                        break;

                                    case "mbike":
                                        comm.Parameters.AddWithValue("@opMacchina", 3);
                                        idPercorso = 3;
                                        break;

                                    case "personalizzato":
                                        comm.Parameters.AddWithValue("@opMacchina", 3);
                                        idPercorso = 3;
                                        break;

                                    default:
                                        break;
                                }
                                
                                //and executing the command
                                comm.ExecuteNonQuery();

                                //and keeping updating the routing 
                                //Welming step
                                comm.Parameters.Clear();
                                comm.Parameters.AddWithValue("@idLotto", idLotto[i]);
                                comm.Parameters.AddWithValue("@idPezzo", idLotto[i] + " - " + (j + 1));
                                comm.Parameters.AddWithValue("@step", 2);
                                comm.Parameters.AddWithValue("@durata", 8);
                                comm.Parameters.AddWithValue("@durataSetUp", 1);
                                comm.Parameters.AddWithValue("@opMacchina", 9);
                                
                                comm.ExecuteNonQuery();
                                //Furnace Step
                                comm.Parameters.Clear();
                                comm.Parameters.AddWithValue("@idLotto", idLotto[i]);
                                comm.Parameters.AddWithValue("@idPezzo", idLotto[i] + " - " + (j + 1));
                                comm.Parameters.AddWithValue("@step", 3);
                                comm.Parameters.AddWithValue("@durata", 8);
                                comm.Parameters.AddWithValue("@durataSetUp", 0);
                                comm.Parameters.AddWithValue("@opMacchina", 10);
                                
                                comm.ExecuteNonQuery();
                                //Painting Step
                                comm.Parameters.Clear();
                                comm.Parameters.AddWithValue("@idLotto", idLotto[i]);
                                comm.Parameters.AddWithValue("@idPezzo", idLotto[i] + " - " + (j + 1));
                                comm.Parameters.AddWithValue("@step", 4);
                                comm.Parameters.AddWithValue("@durata", 5);
                                comm.Parameters.AddWithValue("@durataSetUp", 2);

                                if (linea[i].CompareTo("pastello") == 0)
                                {
                                    comm.Parameters.AddWithValue("@opMacchina", 11);
                                }
                                else if (linea[i].CompareTo("metallizzato") == 0)
                                {
                                    comm.Parameters.AddWithValue("@opMacchina", 12);
                                }

                                comm.ExecuteNonQuery();
                                //Drying Step
                                comm.Parameters.Clear();
                                comm.Parameters.AddWithValue("@idLotto", idLotto[i]);
                                comm.Parameters.AddWithValue("@idPezzo", idLotto[i] + " - " + (j + 1));
                                comm.Parameters.AddWithValue("@step", 5);
                                comm.Parameters.AddWithValue("@durata", 6);
                                comm.Parameters.AddWithValue("@durataSetUp", 0);
                                comm.Parameters.AddWithValue("@opMacchina", 13);

                                comm.ExecuteNonQuery();
                                //Assembling Step
                                comm.Parameters.Clear();
                                comm.Parameters.AddWithValue("@idLotto", idLotto[i]);
                                comm.Parameters.AddWithValue("@idPezzo", idLotto[i] + " - " + (j + 1));
                                comm.Parameters.AddWithValue("@step", 6);
                                comm.Parameters.AddWithValue("@durata", 4);
                                comm.Parameters.AddWithValue("@durataSetUp", 1);
                                comm.Parameters.AddWithValue("@opMacchina", 14);
                                
                                comm.ExecuteNonQuery();

                                //waiting until the stuff passes under the Quality Control Area
                                Thread.Sleep(5000);

                                //creating the assignment path-vehicle
                                query = "INSERT INTO dbo.percorsiveicoli (idPercorso, idVeicolo, tempoAssegnazione, tempoPartenza) VALUES (@idPercorso, @idVeicolo, @tempoAssegnazione, @tempoPartenza)";
                                comm = new SqlCommand(query, conn);
                                comm.Parameters.Clear();
                                string idVeicolo = "AGV" + idPercorso.ToString();
                                comm.Parameters.AddWithValue("@idPercorso", idPercorso);
                                comm.Parameters.AddWithValue("@idVeicolo", idVeicolo);
                                comm.Parameters.AddWithValue("@tempoAssegnazione", DateTime.Now);
                                comm.Parameters.AddWithValue("@tempoPartenza", DateTime.Now);
                                
                                comm.ExecuteNonQuery();
                                //and getting the id of that assignment
                                query = "SELECT TOP 1 id FROM dbo.percorsiveicoli ORDER BY id DESC";
                                comm = new SqlCommand(query, conn);
                                comm.Parameters.Clear();
                                
                                reader = comm.ExecuteReader();
                                reader.Read();

                                int idAssegnazione = (int)reader["id"];

                                reader.Close();


                                //Going for the Laser Cut then
                                //Console.WriteLine("LASER CUT");

                                query = "UPDATE stodb.dbo.statoordini SET stato = @stato WHERE idLotto = @idLotto";
                                comm = new SqlCommand(query, conn);
                                comm.Parameters.Clear();
                                comm.Parameters.AddWithValue("@stato", "cutting");
                                comm.Parameters.AddWithValue("@idLotto", idLotto[i]);

                                comm.ExecuteNonQuery();


                                for (int k = 0; k < quantitaTubi[i]; k++)
                                {
                                    //updating the lasercut table for each tube
                                    query = "INSERT INTO dbo.lasercutdp (codiceTubo, idAssegnazione, startTime) VALUES (@codiceTubo, @idAssegnazione, @startTime)";
                                    comm = new SqlCommand(query, conn);
                                    comm.Parameters.Clear();
                                    comm.Parameters.AddWithValue("@codiceTubo", codiceBarre[k]);
                                    comm.Parameters.AddWithValue("@idAssegnazione", idAssegnazione);
                                    comm.Parameters.AddWithValue("@startTime", DateTime.Now);
                                    
                                    comm.ExecuteNonQuery();
                                }
                                if (flag)
                                {
                                    //updating the interested LC's queue
                                    if (rInt == 1)
                                    {
                                        _queueLC1.Enqueue(codiceBarre);
                                        _queueLC1.Enqueue(idLotto[i]);
                                        _queueLC1.Enqueue(idAssegnazione);
                                        //signaling the service after the laser cut.
                                        _signalLC1.Set();
                                    }
                                    else if (rInt == 2)
                                    {
                                        _queueLC2.Enqueue(codiceBarre);
                                        _queueLC2.Enqueue(idLotto[i]);
                                        _queueLC2.Enqueue(idAssegnazione);
                                        _signalLC2.Set();

                                    }
                                    else
                                    {
                                        _queueLC3.Enqueue(codiceBarre);
                                        _queueLC3.Enqueue(idLotto[i]);
                                        _queueLC3.Enqueue(idAssegnazione);
                                        _signalLC3.Set();
                                    }

                                }
                                else
                                {
                                    _queueLC3.Enqueue(codiceBarre);
                                    _queueLC3.Enqueue(idLotto[i]);
                                    _queueLC3.Enqueue(idAssegnazione);
                                    //signaling the service after the laser cut.
                                    _signalLC3.Set();
                                }

                            }
                            else
                            {
                                //launch exception on storage
                                Console.WriteLine("NOT ENOUGH RAW MATERIALS");
                                //and waiting for someone inserting some.

                                // launch a signal to set an error in the UI
                                _signalErrorRM.Set();
                                // wait for new raw material
                                _signalError.WaitOne();
                                Console.WriteLine("NEW RAW MATERIALS RECEIVED");
                            }
                            //at the end of the routing for this frame, we check whenever the quantity has decreased or not
                            query = "SELECT quantitaDesiderata FROM dbo.statoordini WHERE idLotto = @idLotto";
                            comm = new SqlCommand(query, conn);
                            SqlDataReader reader1;
                            comm.Parameters.Clear();
                            comm.Parameters.AddWithValue("@idLotto", idLotto[i]);

                            reader1 = comm.ExecuteReader();
                            reader1.Read();
                            int quantNew = (int)reader1["quantitaDesiderata"];
                            //and if it is, then let's set the new quantity;
                            if (quantNew < quantitaOrdine[i])
                            {
                                quantitaOrdine[i] = quantNew;
                            }
                            reader1.Close();
                        }
                    }
                    //sleeping the thread for 2 secs
                    Thread.Sleep(2000);
                }
            }
        }
    }
}
