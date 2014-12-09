using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;

namespace MRP.Common.DAF
{
    public class SqlServerDAF
    {
        private const int DEFAULT_TIMEOUT = 30;

        public static int CommandTimeout
        {
            get
            {
                int timeOut = DEFAULT_TIMEOUT;
                object keyTimeout = ConfigurationManager.AppSettings["ConnectionString.TimeOut"];

                if (keyTimeout != null)
                {
                    timeOut = Convert.ToInt16(keyTimeout.ToString());
                }

                return timeOut;
            }
        }

        #region Trace Functions
        /// <summary>
        /// This method will write a trace warning to the screen for developers to
        /// nail down any bottlenecks that might be occurring in stored procedures.
        /// In order to view this display, the developer will need to place <code>
        /// Page.Trace.IsEnabled = true;</code> in their page_load method.
        /// </summary>
        /// <param name="cmd">The SqlCommand that is currently being run.</param>
        private static void TraceDBCalls(SqlCommand cmd, string objectType)
        {
            string parameterText = string.Empty;
            string databaseText = cmd.Connection.Database.ToString();
            string serverText = cmd.Connection.DataSource.ToString();
            string commandText = cmd.CommandText.ToString();
            int paramCount = 0;

            string sString = "";

            commandText = commandText.Replace("dbo.[", "").Replace("]", "");

            string parmValue = string.Empty;

            try
            {
                foreach (SqlParameter param in cmd.Parameters)
                {
                    if (param.Direction != ParameterDirection.ReturnValue)
                    {

                        if (paramCount != 0)
                        {
                            parameterText += ", ";
                        }
                        //if (oParam.SqlDbType == SqlDbType.Varchar || oParam.SqlDbType == SqlDbType.Char || oParam.SqlDbType == SqlDbType.DateTime)
                        if (param.SqlDbType == SqlDbType.VarChar || param.SqlDbType == SqlDbType.Date)
                        {
                            sString = "'";
                        }
                        else
                        {
                            sString = "";
                        }
                        if (param.Value != null)
                        {
                            parmValue = param.Value.ToString();
                        }
                        else
                        {
                            parmValue = "NULL";
                            sString = ""; //don't wrap Null value in single quote
                        }

                        parameterText += param.ParameterName + "=" + sString + parmValue + sString;
                        paramCount += 1;
                    }
                }

                string strProcCmd = "EXECUTE " + commandText + " " + parameterText + " " +
                    databaseText + " " + serverText + " " + objectType;

                //TEMP - PLACE PROC CALL TO SESSION
                if (HttpContext.Current.Session != null)
                {
                    HttpContext.Current.Session["PROC"] = strProcCmd;
                }

                //WriteToTraceTable("EXECUTE", CommandText, ParameterText, DataBaseText, ServerText, strObjectType, "N/A");
            }
            finally { }
        }
        #endregion

        /// <summary>
        /// Used for an insert where identity column for 
        /// new record needs to be returned.
        /// </summary>
        /// <param name="cmd">
        /// Command object to be created then passed in.
        /// </param>
        /// <returns>
        /// Integer value of the new row inserted
        /// </returns>
        /// <remarks>
        /// This method was specifically created for tables using
        /// an <b>int</b> or <b>bigint</b> as primary key.
        /// Tables using a unique identifier for 
        /// primary key will need to be contructed using 
        /// runProcedure() method.
        /// </remarks>
        public static int GetIdentityId(SqlCommand cmd)
        {
            int result = -1;

            try
            {
                cmd.CommandTimeout = CommandTimeout;

                if (cmd.Connection.State == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }

                int? id = (int?)cmd.ExecuteScalar();
                if (id.HasValue)
                {
                    result = id.Value;
                }

                return result;
            }
            finally
            {
                if (cmd.Connection.State == ConnectionState.Open)
                {
                    cmd.Connection.Close();
                }

                cmd.Connection.Dispose();
            }
        }
        /// <summary>
        /// Retrieves a filled dataset from a command object
        /// and names the table with the passed in table name.
        /// </summary>
        /// <param name="cmd">
        /// Command object to be created then passed in.
        /// </param>
        /// <param name="sTableName">
        /// Table name to be used within the dataset.
        /// </param>
        /// <returns>
        /// A populated dataset with a table named with the table
        /// name passed in.
        /// </returns>
        public static DataSet GetDataSet(SqlCommand cmd, string tableName)
        {
            DataSet ds = new DataSet();
            SqlDataAdapter adapter = new SqlDataAdapter();

            try
            {
                cmd.CommandTimeout = CommandTimeout;

                if (cmd.Connection.State == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }

                adapter.SelectCommand = cmd;
                adapter.Fill(ds, tableName);
            }
            finally
            {
                if (cmd.Connection.State == ConnectionState.Open)
                {
                    cmd.Connection.Close();
                }

                cmd.Connection.Dispose();
                adapter.Dispose();
            }

            return ds;
        }
        /// <summary>
        /// Retrieves a filled datareader from a command object
        /// and leaves the connection open until the object is
        /// disposed of.
        /// </summary>
        /// <param name="cmd">
        /// Command object to be created then passed in.
        /// </param>
        /// <returns>
        /// A populated datareader in an <b>open</b> state.
        /// </returns>
        /// <remarks>
        ///	This datareader object <b><u>must</u></b> be disposed
        ///	of, especially within a SyBase environment.  If not
        ///	closed, a connection will persist using up valuable
        ///	resources.
        /// </remarks>
        public static SqlDataReader GetDataReader(SqlCommand cmd)
        {
            SqlDataReader reader;

            try
            {
                cmd.CommandTimeout = CommandTimeout;

                if (cmd.Connection.State == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }

                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
            finally
            {
                if (cmd.Connection.State == ConnectionState.Open)
                {
                    cmd.Connection.Close();
                }

                cmd.Connection.Dispose();
            }

            return reader;
        }
        /// <summary>
        /// Retrieves a filled dataview from a dataset object.
        /// This object is a disconnected representation of
        /// the data.
        /// </summary>
        /// <param name="cmd">
        /// Command object to be created then passed in.
        /// </param>
        /// <returns>
        /// A populated dataview.
        /// </returns>
        /// <remarks>
        /// The object returned can be sorted and filtered.
        /// </remarks>
        public static DataView GetDataView(SqlCommand cmd)
        {
            DataView dv;
            DataSet ds = new DataSet();

            ds = GetDataSet(cmd, "Table");
            dv = new DataView(ds.Tables["Table"]);

            ds.Dispose();
            return dv;
        }
        /// <summary>
        /// This method returns the first data point in a resultset.
        /// Column 0, Row 0.  Best used in an aggregate query.  
        /// </summary>
        /// <param name="cmd">
        /// Command object to be created then passed in.
        /// </param>
        /// <returns>
        /// Value of float returned from resultset
        /// </returns>
        public static string GetScalerValue(SqlCommand cmd)
        {
            object temp;
            string result = string.Empty;

            try
            {
                cmd.CommandTimeout = CommandTimeout;
                if (cmd.Connection.State == ConnectionState.Closed)
                { cmd.Connection.Open(); }

                temp = cmd.ExecuteScalar();

                if (temp != null)
                {
                    result = temp.ToString();
                    temp = null;
                }
                return result;
            }
            finally
            {
                if (cmd.Connection.State == ConnectionState.Open)
                {
                    cmd.Connection.Close();
                }

                cmd.Connection.Dispose();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        public static int ExecProcedure(SqlCommand cmd)
        {
            try
            {
                cmd.CommandTimeout = CommandTimeout;

                if (cmd.Connection.State == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }

                return cmd.ExecuteNonQuery();
            }
            finally
            {
                if (cmd.Connection.State == ConnectionState.Open)
                {
                    cmd.Connection.Close();
                }

                cmd.Connection.Dispose();
            }
        }
        public static string ExecProcedureWithErrorReturn(SqlCommand cmd)
        {
            string error = string.Empty;

            try
            {
                cmd.CommandTimeout = CommandTimeout;

                if (cmd.Connection.State == ConnectionState.Closed)
                {
                    cmd.Connection.Open();
                }

                cmd.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                error = e.Message;
            }
            finally
            {
                if (cmd.Connection.State == ConnectionState.Open)
                {
                    cmd.Connection.Close();
                }

                cmd.Connection.Dispose();
            }

            return error;
        }
        private static void RefreshWebConfig()
        {
            // Create the file and clean up handles.
            string path = HttpContext.Current.Request.ServerVariables["APPL_PHYSICAL_PATH"].ToString();
            string fileSource = path + "web.config_livebackup";
            string fileTarget = path + "web.config";

            //using (System.IO.FileStream fs = System.IO.File.Create(sPath)) { }

            System.IO.File.Copy(fileSource, fileTarget, true);
        }
    }

}
