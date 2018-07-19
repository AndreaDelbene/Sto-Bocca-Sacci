﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DibrisBike
{
    class Routing
    {
        public Routing()
        {
        }

        public void routingMagazzino(SqlConnection conn, ConcurrentQueue<object> _queue, AutoResetEvent _signal)
        {
            while (true)
            {
                //waiting until some data comes from the queue
                _signal.WaitOne();
                //getting it then
                object id, tubi;
                _queue.TryDequeue(out id);
                _queue.TryDequeue(out tubi);
                int[] idLotto = (int[])id;
                int[] quantitaTubi = (int[])tubi;

                for (int i = 0; i < idLotto.Length; i++)
                {
                    //and checking, for each request, whenever I have still tubes in the storage
                    string query = "SELECT TOP @quantita FROM stodb.dbo.magazzinomateriali";
                    SqlCommand comm = new SqlCommand(query, conn);

                    comm.Parameters.AddWithValue("@quantita", quantitaTubi[i]);

                    SqlDataAdapter adapter = new SqlDataAdapter(comm);
                    conn.Open();

                    comm.ExecuteNonQuery();

                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    //If i have it, i proceed in updating the 'routing' table
                    if (table.Rows.Count >= quantitaTubi[i])
                    {
                        //TODO
                    }
                    conn.Close();
                }

                Thread.Sleep(2000);
            }
        }
    }
}
