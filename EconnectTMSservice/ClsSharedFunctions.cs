using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using System.Configuration;
using System.Xml;
using Microsoft.Win32;
using System.Threading;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Linq;
using System.IO;

namespace EconnectTMSservice
{
    class ClsSharedFunctions
    {
        ClsEbankingconnections Clogic = new ClsEbankingconnections();


        private string DSTV   = ConfigurationManager.AppSettings["DSTV"];
        private string GOTV  = ConfigurationManager.AppSettings["GOTV"];
        private string ELEC  = ConfigurationManager.AppSettings["ELEC"];
        private string WATR = ConfigurationManager.AppSettings["WATR"];
        private string STTV = ConfigurationManager.AppSettings["STTV"];

        private string TM  = ConfigurationManager.AppSettings["TM"];
        private string TT = ConfigurationManager.AppSettings["TT"];
        private string TA = ConfigurationManager.AppSettings["TA"];

   
        public Dictionary<string, string> ExtractRequests(string message)
        {

            Dictionary<string, string> data = new Dictionary<string, string>();
            string Procode = "";
            string[] strmessage;


            try
            {
                strmessage = message.Split('#');

                Procode = strmessage[0].Substring(0, 2);
                Operation pcode = (Operation)Enum.Parse(typeof(Operation), Procode, true);

                //switch (pcode)
                //{

                //}
                //extract based on the processing code

                return data;
            }
            catch (Exception ex)
            {
                return data;
            }
        }
        //create message
       
        //
        public string CreateXMLMessage(Dictionary<string, string> InMessage)
        {
            string xmlstring = "";


            char xmlquotes = Strings.ChrW(34);


            try
            {

                xmlstring = @"<?xml version=" + xmlquotes + "1.0" + xmlquotes + " encoding=" + xmlquotes + "utf-8" + xmlquotes + "?> \r\n" +
                                 "<message> \r\n" + "<isomsg direction=" + xmlquotes + "response" + xmlquotes + ">\r\n";

                foreach (string kk in InMessage.Keys)
                {
                    if (kk.Contains("field"))
                    {
                        if (string.IsNullOrEmpty(InMessage[kk]) != true)
                        {
                            xmlstring = xmlstring + @"<field id=" + xmlquotes + kk.Replace("field", "") + xmlquotes + " value=" + xmlquotes + InMessage[kk] + xmlquotes + "/>\r\n";
                        }

                    }


                }
                xmlstring = xmlstring + "</isomsg> \r\n" +
                                        "</message>" +
                                           "";



                return xmlstring;
                
            }
            catch (Exception ex)
            {
                //  econnect.classes.clsmain.LogErrorMessage_Ver1(ex, "CreateXMlMessage")
                return "";
            }
        }
        //


        public Dictionary<string, string> ExtractXMLFields(string IncomingMessage)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            try
            {
                //  string cutmsg = inmessage;
                string[] MessageX = IncomingMessage.Split('^');
                string MessageGUID = "";
                // int k = 0;

                IncomingMessage = MessageX[0];
                if (MessageX.Length > 1)
                {
                    MessageGUID = MessageX[1];
                }

                data["Message"] = IncomingMessage;


                XmlTextReader MyReader = new XmlTextReader(new System.IO.StringReader(IncomingMessage));
                while (MyReader.Read())
                {
                    switch (MyReader.NodeType)
                    {
                        case System.Xml.XmlNodeType.Element:
                            if (MyReader.HasAttributes)
                            {
                                string name = MyReader.Name.ToString();
                                string value = MyReader.Value.ToString();
                                string idtag = "";
                                string idvalue = "";
                                if (MyReader.AttributeCount > 1)
                                {
                                    idtag = MyReader.GetAttribute(0);
                                    idvalue = MyReader.GetAttribute(1);
                                }

                                data.Add(name + idtag, idvalue);
                            }


                            break; // TODO: might not be correct. Was : Exit Select

                            
                        case System.Xml.XmlNodeType.Text:


                            break; // TODO: might not be correct. Was : Exit Select


                            
                        case System.Xml.XmlNodeType.EndElement:

                         
                            break;
                        case System.Xml.XmlNodeType.None:
                            //c = c;
                           

                            break;
                        default:
                            break; // TODO: might not be correct. Was : Exit Select


                    }
                }


                MyReader.Close();

            }
            catch (Exception ex)
            {

                // econnect.classes.clsmain.LogErrorMessage(ex, "ExtractXMLFields ", True, data)
            }




            //data.Add("1", "1");
            return data;
        }

        public bool fn_find_client(string strIPAddress)
        {
            try
            {
                string strIPpresent = Clogic.RunStringReturnStringValue("select count(*) as _count from tbPOSDevices where Card_IP_Address='" + strIPAddress + "' OR IPAdd2='" + strIPAddress + "'");
                if (strIPpresent == "1")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public string GetRRNFRomregistry()
        {
            RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            string value64 = "";
            string strRegStatus = "";

            string NXTRRN = "";
            try
            {
                localKey = localKey.OpenSubKey("SOFTWARE\\Eclectics International Ltd\\TMS", true);
                if (localKey != null)
                {
                    // value64 = localKey.GetValue("Use PASV").ToString();
                    strRegStatus = localKey.GetValue("RRNTEST", "").ToString();

                    int status = Convert.ToInt32(strRegStatus);

                    if (string.IsNullOrEmpty(strRegStatus))
                    {
                        status = 0;
                        //My.Computer.Registry.SetValue("",
                    }

                    status = status + 1;


                    NXTRRN = status.ToString();
                    NXTRRN = NXTRRN.PadLeft(12, '0');

                    //WRITE THE RETURNED SEQUENCE IN REGISTRY
                    localKey.SetValue("RRNTEST", status);
                    return NXTRRN;

                }
                else
                {
                    return "-1";

                }



            }
            catch (Exception ex)
            {
                return "-1";
            }

        }


        public string strResponseHeader(string strTerminalid)
        {
            try
            {
                string strResponse = "";

                string strAgentName = Clogic.RunStringReturnStringValue("SELECT agent from tbAgentRegistration where POSDevice='" + strTerminalid + "'");
                string strAgentNumber = Clogic.RunStringReturnStringValue("SELECT agentno from tbAgentRegistration where POSDevice='" + strTerminalid + "'");
                string strAgentLocation = Clogic.RunStringReturnStringValue("SELECT PhysicalAddress from tbAgentRegistration where POSDevice='" + strTerminalid + "'");

                strResponse = "---------Atlantis Microfinance----------" + "#";
                strResponse += "POS Serial:" + strTerminalid + "#";
                //strResponse += "Car Reg No: KBD 555A" & "#" ' & strTerminalid & "#"   
                strResponse += strTerminalid + "#";
                strResponse += "--------------------------------" + "#";
                string strTime = Strings.Format(DateAndTime.Now, "HH:mm:ss");
                string strDate = Strings.Format(DateAndTime.Now, "dd/MM/yyyy");
                strResponse += "Date :" + strDate + " " + strTime + "#";
                //strResponse += "Time :" & strTime & "#"
                return strResponse;
            }
            catch (Exception ex)
            {
                return "Error in Response Header";
            }
        }

        //

        public string GetFailedPinCodeDescriptions(string strcode)
        {
            string strDescription = "";

            try
            {
                SqlCommand Command = default(SqlCommand);
                SqlDataAdapter adapter = default(SqlDataAdapter);
                DataTable dt = default(DataTable);
                SqlConnection ConnStoredProcedures = new SqlConnection(EconnectTMSservice.sharedvars.eBankConnectionString);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }
                Command = new SqlCommand("SP_GettbFailedPINdescriptions", ConnStoredProcedures);
                Command.CommandType = CommandType.StoredProcedure;
                Command.Parameters.AddWithValue("@code", strcode);
                adapter = new SqlDataAdapter(Command);
                dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    strDescription = dt.Rows[0]["Description"].ToString();
                }
                else
                {
                    strDescription = "Unable to verify PIN";
                }
                adapter.Dispose();
                ConnStoredProcedures.Close();

                return strDescription;
            }
            catch (Exception ex)
            {
                return "";
            }

        }

        public string GETACOUNTFROMECONNECT(string pan, string strField37)
        {
            SqlCommand Command = default(SqlCommand);
            SqlDataAdapter adapter = default(SqlDataAdapter);
            DataTable dt = default(DataTable);
            string AccountNo = "";

            try
            {
                SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.StrEconnectString);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }

                Command = new SqlCommand("SP_GET_AccountNoFromSwitch", ConnStoredProcedures);
                Command.CommandType = CommandType.StoredProcedure;
                Command.Parameters.AddWithValue("PAN", pan);
                Command.Parameters.AddWithValue("strField37", strField37);
                adapter = new SqlDataAdapter(Command);
                dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    AccountNo = dt.Rows[0][0].ToString();
                }
                adapter.Dispose();
                ConnStoredProcedures.Close();
                return AccountNo;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public string strResponseFooter(string strAgentID="")
        {
            try
            {
                string strResponse = "";
                //get the crew details
                if (string.IsNullOrEmpty(strAgentID) == false)
                {
                    string strLogin = "Select FULLNAME from VW_AGENTDETAILS where AgentNo ='" + strAgentID + "'";
                    string AGENTNAME = Clogic.RunStringReturnStringValue(strLogin);
                    strResponse += "You were served by:" + AGENTNAME + "#";
                }


                strResponse += "--------------------------------" + "#";
                strResponse += "  Thank you for Banking with   " + "#";
                strResponse += "    Atlantis Micro Finance      " + "#";
                strResponse += "  Enquries Call: 0722980980    " + "#";
                strResponse += " " + "#";
                strResponse += " " + "#";
                strResponse += " " + "#";
                strResponse += " " + "#";
                strResponse += " " + "#";
                strResponse += " " + "#";

                return strResponse;
            }
            catch (Exception ex)
            {
                return "Footer Error"+ ex.Message;
            }
        }

        public void SPInsertFailedCardPINS(string Field2, string field39, string field37)
        {
            try
            {
                SqlConnection ConnStoredProcedures = new SqlConnection(EconnectTMSservice.sharedvars.eBankConnectionString);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }

                SqlCommand cmdInsert = new SqlCommand();
                cmdInsert.Connection = ConnStoredProcedures;

                SqlParameter param = null;

                cmdInsert.CommandText = "SP_tbFailedCardPIN";
                cmdInsert.CommandType = CommandType.StoredProcedure;
                param = cmdInsert.Parameters.AddWithValue("@CardNumber", Field2);
                param = cmdInsert.Parameters.AddWithValue("@Responsefromcr2", field39);
                param = cmdInsert.Parameters.AddWithValue("@field37", field37);
                int success = cmdInsert.ExecuteNonQuery();

                cmdInsert.Dispose();
                ConnStoredProcedures.Close();
                ConnStoredProcedures.Dispose();


            }
            catch (Exception ex)
            {
            }

        }

        public string fn_RemoveNon_Numeric(string strExpression)
        {
            string strtocheck = Strings.Trim(strExpression);

            char[] myChars = strtocheck.ToCharArray();
            string strNumeic = "";

            try
            {
                // Loop through the array testing if each is a digit
                foreach (char ch in myChars)
                {
                    if (char.IsDigit(ch))
                    {
                        // If it is a digit, show it in a messagebox
                        // (here is where you would save the number to array, variable or whatever)
                        strNumeic = strNumeic + ch;
                        //MessageBox.Show(ch)
                    }
                }
                return strNumeic;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public string GetWorkingdate()
        {
            string sql = "";
            string workingdate = "";
            SqlDataReader RD = default(SqlDataReader);

            try
            {
                sql = "select WorkingDate from tbDateSettings";
                workingdate = Clogic.RunStringReturnStringValue(sql);
                return workingdate;
            }
            catch (Exception ex)
            {
                
                return "";
            }
        }

        public string GetAccountName(string AccountNo)
        {
            string sql = "";
            string accountName = "";

            try
            {
                sql = "SELECT ISNULL(AccountName,'') from tbCustomers WHERE CustomerNo='" + AccountNo.Substring(4,7) + "'";
                accountName = Clogic.RunStringReturnStringValue(sql);
                return accountName;
            }
            catch (Exception ex)
            {

                return "";
            }
        }

        public string GetTellerAllocatedAmount(string Tellerid,string TellerAmount)
            
        {
            string StrResponse = "";
            string sql = "";
            string workingdate = "";
            string id = "";

            SqlDataReader RD = default(SqlDataReader);

            try
            {
                workingdate = GetWorkingdate();
                sql = "select *  from tbTellerDenominations where ToTeller=1 and TellerAccepted=0 and ToTellerID='" + Tellerid + "' AND WorkingDate='" + workingdate + "' and TotalAmount=" + TellerAmount + "";

                RD = Clogic.RunQueryReturnDataReader(sql);
                if (RD.HasRows)
                {
                    RD.Read();
                    StrResponse = RD["ID"].ToString();
                }
                else
                {
                    StrResponse = "";
                }
                return StrResponse;
            }
            catch (Exception ex)
            {
                return StrResponse;
            }
        }
        //
        public bool fn_get_Supervisor(string supervisorID, string password)
        {
            bool Success = false;
            try
            {
                string strSuper = "";
                string Converpass = "";
                // Converpass = md5ThumbiKalazaa(password)

                SqlDataReader supervisorRD = default(SqlDataReader);
                string STRSQL = "";
                MySecurity.CryptoFactory CrytographFactory = new MySecurity.CryptoFactory();
                MySecurity.ICrypto Crytographer = CrytographFactory.MakeCryptographer();

                string MyPassword = Crytographer.Encrypt(password);

                STRSQL = "select SupervisorID,Password  from tbSupervisors where supervisorID='" + supervisorID + "' AND Password='" + MyPassword + "'";

                supervisorRD = Clogic.RunQueryReturnDataReader(STRSQL);
                if (supervisorRD.HasRows)
                {
                    supervisorRD.Read();
                    Success = true;
                }
                else
                {
                    Success = false;
                }
                return Success;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        //
        
    public string fn_get_SupervisorFloatAccount(string Supervisorid ) 
    {
        string FloatAccount  = "";
        SqlDataReader supervisorRD ; 
        string STRSQL  = "";

        try
        {
            STRSQL = "select FloatAccount,SupervisorID,Password  from tbSupervisors where supervisorID='" + Supervisorid + "'";

            supervisorRD = Clogic.RunQueryReturnDataReader(STRSQL);
            if(supervisorRD.HasRows)
            {
                supervisorRD.Read();
                FloatAccount = supervisorRD["FloatAccount"].ToString();
                return FloatAccount;
            }
            else
            {
                FloatAccount = "";
                return FloatAccount;
            }
        }catch(Exception ex)
        {
            return "";
        }
        

        }
        //
        public string fn_getAgentLocation(string strAgentID )
        {
        string strLocation = "";
        string strLogin = "";

        try
        {
            strLogin = "Select Address from tbAgentRegistration where AgentNo ='" + strAgentID + "'";
            strLocation = Clogic.RunStringReturnStringValue(strLogin);
            return strLocation;
        }catch(Exception ex)
        {
            return "";
        }
       
        }
        //
        public double GetCashpickupCallnumber(string Callnumber)
        {
            SqlCommand Command = default(SqlCommand);
            SqlDataAdapter adapter = default(SqlDataAdapter);
            DataTable dt = default(DataTable);
            double amount = 0;

            try
            {
                SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.strConnectionString_);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }
                Command = new SqlCommand("SP_Get_cashpickup_Trans", ConnStoredProcedures);
                Command.CommandType = CommandType.StoredProcedure;
                Command.Parameters.AddWithValue("CallNumber", Callnumber);
                adapter = new SqlDataAdapter(Command);
                dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    amount =double.Parse(dt.Rows[0]["amount"].ToString());
                }
                else
                {
                    amount = 0;
                }
                adapter.Dispose();
                ConnStoredProcedures.Close();
                return amount;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        //
        public string Get_CashpowerResponse(string strField37)
        {
            SqlCommand Command = default(SqlCommand);
            SqlDataAdapter adapter = default(SqlDataAdapter);
            DataTable dt = default(DataTable);
          
            string strToken = "";

            try
            {
                //KICHE FIX TO DEAL WITH SWITCH ON TEST
                SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.StrEconnectString);
                //SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.StrEconnectTestString);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }
                Command = new SqlCommand("SP_GET_Cashpower_RESPONSE", ConnStoredProcedures);
                Command.CommandType = CommandType.StoredProcedure;
                Command.Parameters.AddWithValue("@Field37", strField37);
                //Command.Parameters.AddWithValue("@procode", strprocode)
                adapter = new SqlDataAdapter(Command);
                dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    strToken = dt.Rows[0]["token"].ToString();                
                   
                }
                else
                {
                    strToken = " ";
                }
                adapter.Dispose();
                ConnStoredProcedures.Close();
                return strToken;
            }
            catch (Exception ex)
            {
                return "Error Occured";
            }
        }

        //get meter consumer name
        public string Get_Cashpoweruser(string strField37)
        {
            SqlCommand Command = default(SqlCommand);
            SqlDataAdapter adapter = default(SqlDataAdapter);
            DataTable dt = default(DataTable);

            string strConsumerName = "";

            try
            {
                //KICHE FIX TO DEAL WITH SWITCH ON TEST
                SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.StrEconnectString);
                //SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.StrEconnectTestString);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }
                Command = new SqlCommand("SP_GET_Cashpower_UserName", ConnStoredProcedures);
                Command.CommandType = CommandType.StoredProcedure;
                Command.Parameters.AddWithValue("@Field37", strField37);
                //Command.Parameters.AddWithValue("@procode", strprocode)
                adapter = new SqlDataAdapter(Command);
                dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    strConsumerName = dt.Rows[0]["ConsumerName"].ToString();

                }
                else
                {
                    strConsumerName = " ";
                }
                adapter.Dispose();
                ConnStoredProcedures.Close();
                return strConsumerName;
            }
            catch (Exception ex)
            {
                return "Error Occured";
            }
        }
        //end consumer name 
        //
        public string GetCallnumberStatus(string Callnumber, string strprocode)
        {
            SqlCommand Command = default(SqlCommand);
            SqlDataAdapter adapter = default(SqlDataAdapter);
            DataTable dt = default(DataTable);
            bool Tickestatus = false;
            bool isClosed = false;
            string strticketstatus = "";

            try
            {
                SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.strConnectionString_);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }
                Command = new SqlCommand("SP_Get_cashpickup_Trans", ConnStoredProcedures);
                Command.CommandType = CommandType.StoredProcedure;
                Command.Parameters.AddWithValue("@CallNumber", Callnumber);
                //Command.Parameters.AddWithValue("@procode", strprocode)
                adapter = new SqlDataAdapter(Command);
                dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    Tickestatus =bool.Parse(dt.Rows[0]["TicketClosed"].ToString());
                    isClosed = bool.Parse(dt.Rows[0]["Closed"].ToString());
                    if (isClosed == false)
                    {
                        strticketstatus= "Ticket Not Authorized :" + Callnumber;
                    }
                    else if (Tickestatus == true)
                    {
                        strticketstatus= "Confirmation Closed For: " + Callnumber;
                    }
                    else
                    {
                        strticketstatus= "ok";
                    }
                }
                else
                {
                    strticketstatus= "Ticket Not Found: " + Callnumber;
                }
                adapter.Dispose();
                ConnStoredProcedures.Close();
                return strticketstatus;
            }
            catch (Exception ex)
            {
                return "Error Occured";
            }
        }
        //
        public string fn_get_AdministratorNames(string strsearch)
        {
            string sqlstr = "";
            string Names = "";

            try
            {
                sqlstr = "select ITEMVALUE FROM tbGENERALPARAMS WHERE ITEMNAME='" + strsearch + "'";
                Names = Clogic.RunStringReturnStringValue(sqlstr);
                return Names;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        //
        public void spInsertPOSTransaction(string Field_0, string Field_2, string Field_3, string Field_4, string Field_7, string Field_11, string Field_12, string Field_24, string Field_32, string Field_37,
string Field_39, string Field_41, string Field_54, string Field_56, string Field_65, string Field_66, string Field_68, string Field_80, string Field_100, string Field_101,
string Field_102, string Field_103, string Field_127)
        {
            try
            {
                SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.strConnectionString_);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }
                Field_0 = Field_0.PadLeft(12, '0');
                Field_37 = Field_0;
                SqlCommand cmdInsert = new SqlCommand();
                cmdInsert.Connection = ConnStoredProcedures;

                SqlParameter param = null;


                if (true)
                {
                    cmdInsert.CommandText = "SP_INSERT_POS_TRANSACTIONS";
                    cmdInsert.CommandType = CommandType.StoredProcedure;
                    param = cmdInsert.Parameters.AddWithValue("@Field_0", Field_0);
                    param = cmdInsert.Parameters.AddWithValue("@Field_2", Field_2);
                    param = cmdInsert.Parameters.AddWithValue("@Field_3", Field_3);
                    param = cmdInsert.Parameters.AddWithValue("@Field_4", Conversion.Val(Field_4));
                    param = cmdInsert.Parameters.AddWithValue("@Field_7", DateTime.Now.ToString("MMddHHmmss"));
                    param = cmdInsert.Parameters.AddWithValue("@Field_11", Field_0);
                    param = cmdInsert.Parameters.AddWithValue("@Field_12", DateTime.Now.ToString("HHmmss"));
                    param = cmdInsert.Parameters.AddWithValue("@Field_24", Field_24);
                    param = cmdInsert.Parameters.AddWithValue("@Field_32", Field_32);
                    param = cmdInsert.Parameters.AddWithValue("@Field_37", Field_37);
                    param = cmdInsert.Parameters.AddWithValue("@Field_39", Field_39);
                    param = cmdInsert.Parameters.AddWithValue("@Field_41", Field_41);
                    param = cmdInsert.Parameters.AddWithValue("@Field_54", Field_54);
                    param = cmdInsert.Parameters.AddWithValue("@Field_56", Field_56);
                    param = cmdInsert.Parameters.AddWithValue("@Field_65", Field_65);
                    param = cmdInsert.Parameters.AddWithValue("@Field_66", Field_66);
                    param = cmdInsert.Parameters.AddWithValue("@Field_68", Field_68);
                    param = cmdInsert.Parameters.AddWithValue("@Field_80", Field_80);
                    param = cmdInsert.Parameters.AddWithValue("@Field_100", Field_100);
                    param = cmdInsert.Parameters.AddWithValue("@Field_101", Field_101);
                    param = cmdInsert.Parameters.AddWithValue("@Field_102", Field_102);
                    param = cmdInsert.Parameters.AddWithValue("@Field_103", Field_103);
                    param = cmdInsert.Parameters.AddWithValue("@Field_127", Field_127);
                    param = cmdInsert.Parameters.AddWithValue("@transaction_date", Strings.Format(DateAndTime.Now, "dd-MMM-yyyy HH:mm:ss.fff"));

                    int success = cmdInsert.ExecuteNonQuery();

                }

                cmdInsert.Dispose();
                ConnStoredProcedures.Close();
                ConnStoredProcedures.Dispose();

            }
            catch (Exception ex)
            {
            }
        }
//
       
        public string GenerateXMLtoeConnect(string strTransactionid, string strMsgid, string strfield2, string strfield3, string strfield4, string strfield24, string strfield41, string strfield65, string strfield66, string strfield100,
string strfield101, string strfield102, string strfield103, string strChannel, string strAgentID, ref Guid myguid, string narration = "", string strfield71 = "", string strfield74 = "", string strfield126 = "",
string strfield6 = "",string strfield60="")
        {

            try
            {
                string strXML = "";
                strTransactionid = strTransactionid.PadLeft(12, '0');
                string strField37 = strTransactionid;
                //Clogic.RunStringReturnStringValue("select NextRRN from tbSequenceValues")
                //Clogic.RunNonQuery("update tbSequenceValues set NextRRN='" & (Double.Parse(strField37) + 1).ToString() & "'")

                strXML = "<?xml version= \"1.0\"  encoding= \"utf-8\"?>" + "<message>" + "<isomsg direction=\"request\">" + 
                    "<field id=\"0\" value=\"" + strMsgid + "\"/>" +
                    "<field id=\"2\" value=\"" + strfield2 + "\"/>" + 
                    "<field id=\"3\" value=\"" + strfield3 + "\"/>" + 
                    "<field id=\"4\" value=\"" + strfield4 + "\"/>" + 
                    "<field id=\"6\" value=\"" + strfield6 + "\"/>" + 
                    "<field id=\"7\" value=\"" + DateTime.Now.ToString("MMddHHmmss") + "\"/>" + 
                    "<field id=\"11\" value=\"" + strTransactionid.Substring(strTransactionid.Length - 6, 6) + "\"/>" + 
                    "<field id=\"12\" value=\"" + DateTime.Now.ToString("HHmmss") + "\"/>" + 
                    "<field id=\"24\" value=\"" + strfield24 + "\"/>" + 
                    "<field id=\"32\" value=\"" + strChannel + "\"/>" + 
                    "<field id=\"37\" value=\"" + strField37.PadLeft(12, '0') + "\"/>" + 
                    "<field id=\"41\" value=\"" + strfield41 + "\"/>" + 
                    "<field id=\"56\" value=\"" + myguid.ToString() + "\"/>" +
                    "<field id=\"60\" value=\"" + strfield60 + "\"/>" + 
                    "<field id=\"65\" value=\"" + strfield65 + "\"/>" + 
                    "<field id=\"66\" value=\"" + strfield66 + "\"/>" + 
                    "<field id=\"68\" value=\"" + narration + "\"/>" + 
                    "<field id=\"71\" value=\"" + strfield71 + "\"/>" + 
                    "<field id=\"74\" value=\"" + strfield74 + "\"/>" + 
                    "<field id=\"100\" value=\"" + strfield100 + "\"/>" + 
                    "<field id=\"101\" value=\"" + strfield101 + "\"/>" + 
                    "<field id=\"102\" value=\"" + strfield102 + "\"/>" + 
                    "<field id=\"103\" value=\"" + strfield103 + "\"/>" + 
                    "<field id=\"126\" value=\"" + strfield126 + "\"/>" + 
                    "</isomsg>" + "</message>";

                //FormatXml(strXML)
                return FormatXml(strXML);
            }
            catch (Exception ex)
            {
                return "";
            }
        }


        private string FormatXml(string sUnformattedXml)
        {
            try
            {
                //load unformatted xml into a dom
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(sUnformattedXml);

                //will hold formatted xml
                StringBuilder sb = new StringBuilder();

                //pumps the formatted xml into the StringBuilder above
                StringWriter sw = new StringWriter(sb);

                //does the formatting
                XmlTextWriter xtw = null;

                try
                {
                    //point the xtw at the StringWriter
                    xtw = new XmlTextWriter(sw);

                    //we want the output formatted
                    xtw.Formatting = Formatting.Indented;

                    //get the dom to dump its contents into the xtw 
                    xd.WriteTo(xtw);
                }
                finally
                {
                    //clean up even if error
                    if (xtw != null)
                    {
                        xtw.Close();
                    }
                }

                //return the formatted xml
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return sUnformattedXml;
            }

        }

       
        //
        public string fn_getTellerAccountNumber(string strTellerID)
        {
            try
            {
                if (strTellerID.Length == 5)
                {
                    strTellerID = "T" + strTellerID;
                }
                else
                {
                    strTellerID = "A" + strTellerID;
                }
                string strLogin = "Select MerchantAccount from VW_AGENTDETAILS where AgentNo ='" + strTellerID + "'";
                string rsReader = Clogic.RunStringReturnStringValue(strLogin);
                return rsReader;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        //
        public string fn_get_kits_price()
        {
            try
            {
                string strKitsPrice = Clogic.RunStringReturnStringValue("select ITEMVALUE  from tbGENERALPARAMS where ITEMNAME='KitsPrice'");
                if (string.IsNullOrEmpty(strKitsPrice))
                {
                    strKitsPrice = "0";
                }
                return strKitsPrice;
            }
            catch (Exception ex)
            {
                return "0";
            }
        }
        //
        public string fn_getTellerTransferAccount(string strTellerID)
        {
            try
            {
                if (strTellerID.Length == 5)
                {
                    strTellerID = "T" + strTellerID;
                }
                else
                {
                    strTellerID = "A" + strTellerID;
                }
                string strLogin = "Select TransferAccount from VW_AGENTDETAILS where AgentNo ='" + strTellerID + "'";
                string rsReader = Clogic.RunStringReturnStringValue(strLogin);
                return rsReader;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        //
        public string fn_getAgentName(string strAgentID)
        {
            string FULLTIME = "";
            string strLogin = "";

            try
            {
                strLogin = "Select FULLNAME from VW_AGENTDETAILS where AgentNo ='" + strAgentID + "'";
                FULLTIME = Clogic.RunStringReturnStringValue(strLogin);
                return FULLTIME;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        //
        public string fn_get_SupervisorName(string strSupervisorID)
        {
            string SupervisorName = "";
            string strSql = "";

            try
            {
                strSql = "select SupervisorName  from tbSupervisors where supervisorID='" + strSupervisorID + "'";

                SupervisorName = Clogic.RunStringReturnStringValue(strSql);
                return SupervisorName;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        //
        public  Boolean GetEnabledServices(string strServiceCode)
        {
        Boolean Strstatus   = false;
        //' 1 MEANS ENABLED 0 MEANS DISABLED
        try{
            switch(strServiceCode)
            {                                
                case "TM":

                    if( TM == "1")
                    {
                        Strstatus = true;
                    }
                    else
                    {
                        Strstatus = false;
                    }
                  break;
                case "TA":
                    if(TA == "1" )
                    {
                        Strstatus = true;
                    }
                   else
                    {
                        Strstatus = false;
                    }
                    break;
                case "TT":
                    if(TT == "1")
                    {
                        Strstatus = true;
                    }
                    else
                    {
                        Strstatus = false;
                    }                 

                    break;

                
                case "DSTV":
                    if( DSTV == "1")
                    {
                        Strstatus = true;
                    }
                    else
                    {
                        Strstatus = false;
                    }
                    break;
                case "GOTV":
                    if( GOTV == "1")
                    {
                        Strstatus = true;
                    }
                    else
                    {
                        Strstatus = false;
                    }                   
                 break;
                case "ELEC":
                 if (ELEC == "1")
                 {
                     Strstatus = true;
                 }
                 else
                 {
                     Strstatus = false;
                 }
                 break;
                case "WATR":
                 if (WATR == "1")
                 {
                     Strstatus = true;
                 }
                 else
                 {
                     Strstatus = false;
                 }
                 break;
                case "STTV":
                 if (STTV == "1")
                 {
                     Strstatus = true;
                 }
                 else
                 {
                     Strstatus = false;
                 }
                 break;
            }
            return Strstatus;
        }catch(Exception ex)
        {
            return false;
        }
        }


        public string strResponseagenetTeller(string strAgentTeller)
        {
            SqlCommand Command = default(SqlCommand);
            SqlDataAdapter adapter = default(SqlDataAdapter);
            SqlCommand CommandBranch = default(SqlCommand);
            SqlDataAdapter adapterBranch = default(SqlDataAdapter);
            DataTable dt = default(DataTable);
            DataTable dtbranch = default(DataTable);
            string strAgentName = "";
            string strAgentNumber = "";
            string strAgentLocation = "";
            string strTellerName = "";
            string branch = "";
            string strTellerLocation = "";


            try
            {
                // julius use stored procedure herer
                //agent [SP_GetAgentDetails]
                //teller [SP_GetTellerDetails]
                string strResponse = "";
                if (strAgentTeller.StartsWith("A"))
                {
                    SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.strConnectionString_);
                    if (ConnStoredProcedures.State == ConnectionState.Closed)
                    {
                        ConnStoredProcedures.Open();
                    }
                    Command = new SqlCommand("SP_GetAgentDetails", ConnStoredProcedures);
                    Command.CommandType = CommandType.StoredProcedure;
                    Command.Parameters.AddWithValue("AgentNo", strAgentTeller);
                    adapter = new SqlDataAdapter(Command);
                    dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        strAgentName = dt.Rows[0]["ShopName"].ToString();
                        strAgentNumber = strAgentTeller;
                        strAgentLocation = dt.Rows[0]["ShopAddress"].ToString();
                    }
                    adapter.Dispose();
                    ConnStoredProcedures.Close();
                    //Dim strAgentName As String = Clogic.RunStringReturnStringValue("SELECT agent from tbAgentRegistration where AgentNo='" & strAgentTeller & "'")
                    //Dim strAgentNumber As String = strAgentTeller 'Clogic.RunStringReturnStringValue("SELECT agentno from tbAgentRegistration where AgentNo='" & strAgentTeller & "'")
                    //Dim strAgentLocation As String = Clogic.RunStringReturnStringValue("SELECT PhysicalAddress from tbAgentRegistration where AgentNo='" & strAgentTeller & "'")

                    strResponse = "---------Atlantis MicroFinance----------" + "#";
                    strResponse += "Atlantis Agent:" + strAgentNumber + "#";
                    strResponse += strAgentName + "#";
                    strResponse += strAgentLocation + "#";
                    strResponse += "--------------------------------" + "#";

                    //means Teller
                }
                else if (strAgentTeller.StartsWith("T"))
                {

                    SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.strConnectionString_);
                    if (ConnStoredProcedures.State == ConnectionState.Closed)
                    {
                        ConnStoredProcedures.Open();
                    }
                    Command = new SqlCommand("SP_GetTellerDetails", ConnStoredProcedures);
                    Command.CommandType = CommandType.StoredProcedure;
                    Command.Parameters.AddWithValue("TellerId", strAgentTeller);
                    adapter = new SqlDataAdapter(Command);
                    dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        strTellerName = dt.Rows[0]["TellerName"].ToString();
                        branch = dt.Rows[0]["branch"].ToString();
                    }
                    adapter.Dispose();
                    // ConnStoredProcedures.Close()
                    if (string.IsNullOrEmpty(branch) == false)
                    {
                        CommandBranch = new SqlCommand("SP_GettbBranchesDetails", ConnStoredProcedures);
                        CommandBranch.CommandType = CommandType.StoredProcedure;
                        CommandBranch.Parameters.AddWithValue("Branchcode", branch);
                        adapterBranch = new SqlDataAdapter(CommandBranch);
                        dtbranch = new DataTable();
                        adapterBranch.Fill(dtbranch);
                        if (dtbranch.Rows.Count > 0)
                        {
                            strTellerLocation = dtbranch.Rows[0]["Location"].ToString();
                        }
                        adapterBranch.Dispose();
                    }

                    adapter.Dispose();
                    ConnStoredProcedures.Close();
                    // Dim strTellerName As String = Clogic.RunStringReturnStringValue("SELECT TellerName from tbTellerUsers where TellerId ='" & strAgentTeller & "'")
                    //Dim branch As String = Clogic.RunStringReturnStringValue("SELECT branch from tbTellerUsers where TellerId ='" & strAgentTeller & "'")
                    //Dim strTellerLocation As String = Clogic.RunStringReturnStringValue("SELECT Location from tbBranches where Branchcode ='" & branch & "'")
                    //[SP_GettbBranchesDetails]

                    strResponse = "---------Atlantis MicroFinance----------" + "#";
                    strResponse += "Atlantis Teller :" + strAgentTeller + "#";
                    strResponse += strTellerName + "#";
                    strResponse += strTellerLocation + "#";
                    strResponse += "--------------------------------" + "#";
                }
                string strTime = Strings.Format(DateAndTime.Now, "HH:mm:ss");
                string strDate = Strings.Format(DateAndTime.Now, "dd/MM/yyyy");
                strResponse += strTime + "#";
                strResponse += strDate + "#";
                return strResponse;
            }
            catch (Exception ex)
            {
                return "Error in Response Header";
            }
        }

        //
        private string MonthName(int Monthid)
        {
            string strResponse = "";
            switch (Monthid)
            {
                case 1:
                    strResponse = "JAN";
                    break;
                case 2:
                    strResponse = "FEB";
                    break;
                case 3:
                    strResponse = "MAR";
                    break;
                case 4:
                    strResponse = "APR";
                    break;
                case 5:
                    strResponse = "MAY";
                    break;
                case 6:
                    strResponse = "JUNE";
                    break;
                case 7:
                    strResponse = "JUL";
                    break;
                case 8:
                    strResponse = "AUG";
                    break;
                case 9:
                    strResponse = "SEP";
                    break;
                case 10:
                    strResponse = "OCT";
                    break;
                case 11:
                    strResponse = "NOV";
                    break;
                case 12:
                    strResponse = "DEC";
                    break;
            }
            return strResponse;
        }

        //
        public Boolean fn_Updateagentpassword(string StrAgentID, string StrPassword)
        {
            bool Success = false;
            try
            {
                string SQL = "";
                MySecurity.CryptoFactory CrytographFactory = new MySecurity.CryptoFactory();
                MySecurity.ICrypto Crytographer = CrytographFactory.MakeCryptographer();

                string MyPassword = Crytographer.Encrypt(StrPassword);

                string strInMsg = "update tbAgentRegistration set password='" + MyPassword + "' where AgentNo='" + StrAgentID + "'";
                Success = Clogic.RunNonQuery(strInMsg);
                if (Success == true)
                {
                    //send the given teller an sms informong hime of the new password set 
                    //get the teller phone number and 
                    string stragentphonenumber = fn_getAgentPhonenumber(StrAgentID);
                    string srAgentname = fn_getAgentName(StrAgentID);
                    dynamic strmessage = "Dear, " + srAgentname + " agent no." + StrAgentID + ", your new POS login password is " + StrPassword + ".Your login Id remains " + StrAgentID;
                    int suc = SendSMS(stragentphonenumber, strmessage, "POS", "", "");
                }

                //fn_getAgentPhonenumber
                return Success;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        //
        private string fn_getAgentPhonenumber(string strAgentID)
        {
            string FULLTIME = "";
            string strLogin = "";

            try
            {
                strLogin = "Select MobileNo from tbAgentRegistration where AgentNo ='" + strAgentID + "'";
                FULLTIME = Clogic.RunStringReturnStringValue(strLogin);
                return FULLTIME;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        //
        private string fn_getTellerPhonenumber(string strTellerID)
        {

            try
            {
                string strLogin = "Select PhoneNumber from tbTellerUsers where TellerId ='" + strTellerID + "'";
                string rsReader = Clogic.RunStringReturnStringValue(strLogin);
                return rsReader;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private string fn_getTellerNames(string strTellerID)
        {

            try
            {
                string strLogin = "Select TellerName from tbTellerUsers where TellerId ='" + strTellerID + "'";
                string rsReader = Clogic.RunStringReturnStringValue(strLogin);
                return rsReader;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        //
        public string GetResponseCode(string strField39)
        {
            string strReasonDescription = "";

            try
            {
                switch (strField39)
                {
                    case "53":
                        strReasonDescription = "No Debit Account Present";
                        break;
                    case "57":
                        strReasonDescription = "Do not Honor. Contact Bank";
                        break;
                    case "12":
                        strReasonDescription = "Account not present";
                        break;
                    case "51":
                        strReasonDescription = "Insuficient funds";
                        break;
                    default:
                        strReasonDescription = "Do not Honor. Contact Bank";
                        break;
                }
                return strReasonDescription;
            }
            catch (Exception ex)
            {
                return "";
            }

        }

        //
        public Boolean UpdateCashpickupCallnumber(string Callnumber)
        {
            SqlCommand Command = default(SqlCommand);
            SqlDataAdapter adapter = default(SqlDataAdapter);
            DataTable dt = default(DataTable);
            double amount = 0;

            try
            {
                SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.strConnectionString_);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }

                SqlCommand cmdInsert = new SqlCommand();
                cmdInsert.Connection = ConnStoredProcedures;

                SqlParameter param = null;

                cmdInsert.CommandText = "SP_Modify_cashpickup_Trans";
                cmdInsert.CommandType = CommandType.StoredProcedure;
                param = cmdInsert.Parameters.AddWithValue("@CallNumber", Callnumber);

                int success = cmdInsert.ExecuteNonQuery();


                cmdInsert.Dispose();
                ConnStoredProcedures.Close();
                ConnStoredProcedures.Dispose();
                if (success > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
                
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //
        public Boolean fn_UpdateTellerPassword(string StrAgentID, string strPassword)
        {
            bool Success = false;
            try
            {
                string SQL = "";
                MySecurity.CryptoFactory CrytographFactory = new MySecurity.CryptoFactory();
                MySecurity.ICrypto Crytographer = CrytographFactory.MakeCryptographer();

                string MyPassword = Crytographer.Encrypt(strPassword);

                string strInMsg = "update tbTellerUsers set Password='" + MyPassword + "' where TellerId='" + StrAgentID + "'";
                Success = Clogic.RunNonQuery(strInMsg);
                if (Success == true)
                {
                    //send the given teller an sms informong hime of the new password set 
                    //get the teller phone number and 
                    string stragentphonenumber = fn_getTellerPhonenumber(StrAgentID);

                    string srAgentname = fn_getAgentName(StrAgentID);
                    string tellername = fn_getTellerNames(StrAgentID);
                    dynamic strmessage = "Dear, " + srAgentname + " teller no." + StrAgentID + ", your new POS login password is " + strPassword + ".Your login Id remains " + StrAgentID;
                    bool suc = SendSMS(stragentphonenumber, strmessage, "POS", "", "");
                }
                return Success;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool GetTellerTillstatus(string TellerID)
        {
            bool status = false;
            try
            {
                string workingdate = GetWorkingdate();

                string sql = "SELECT TOP 1 A.CashAccount FROM tbTellerTill A INNER JOIN tbTellerDenominations B ON A.TellerID = B.ToTellerID " +
                                "WHERE A.Active=1 and A.TellerID ='" + TellerID + "' and A.TillOpen=1 AND B.WorkingDate='" + workingdate + "' and B.TellerAccepted=1";
                string TillStatus = Clogic.RunStringReturnStringValue(sql);
               // string TillStatus = Clogic.RunStringReturnStringValue("select distinct b.id,a.WorkingDate,a.ToTeller,a.ReferenceNo,a.CurrencyCode,a.FiveThousand,a.TwoThousand,a.OneThousand,a.FiveHundred,a.OneHundred,a.Fifty,a.TotalAmount, b.* from tbTellerDenominations a inner join tbTellerTill b on a.ToTellerID=b.TellerId where b.Active=1 and a.ToTellerID ='" + TellerID + "' and b.TillOpen=1 and a.WorkingDate ='" + GetWorkingdate() + "'");
                if (string.IsNullOrEmpty(TillStatus.ToString()))
                {
                    status = false;
                }
                else
                {
                    status = true;
                }
                return status;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        //
        public int SendSMS(string PhoneNumber, string strMessage, string channel, string strTransactionReferenceNo, string strAccountNumber)
        {

            try
            {
                string strInsertMsg = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " + " values ('" + PhoneNumber + "','" + strTransactionReferenceNo + "','" + strAccountNumber + "','" + strMessage + "','POS',0,0)";
                bool blninmsg = Clogic.RunNonQuery(strInsertMsg);
                if (blninmsg == true)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        //
        public string fn_getAgentTellerEODTransactions(string strTellerAgent)
        {
            string strResponse = "";
            string SQL = "";
            System.DateTime Today = System.DateTime.Today;
            string Day = DateTime.Now.Day.ToString();
            string Year = DateTime.Now.Year.ToString();
            int Month = DateTime.Now.Month;
            string strdate = "";
            string strmanth = MonthName(Month);
            double amount = 0;
            string strStatementPrinting = "";
            SqlDataReader RSreader = default(SqlDataReader);
            int i = 1;

            try
            {
                strdate = Day.PadLeft(2, '0') + " " + strmanth + " " + Year;

                SQL = "Select Channel,DRCR,TrxRefNo,Amount,TrxDate,CreatedBy,Narration,AccountNo FROM VW_TELLERAGENT_TRX";
                SQL = SQL + " where Channel='POS' AND CreatedBy='" + strTellerAgent + "' AND TrxDate='" + strdate + "'";
                RSreader = Clogic.RunQueryReturnDataReader(SQL);
                strResponse = "--------------------------------" + "#";
                if (RSreader.HasRows)
                {
                    while ((RSreader.Read()))
                    {
                        // strResponse += Convert.ToString(i)
                        amount =double.Parse(RSreader["Amount"].ToString());
                        strStatementPrinting += " ";
                        strStatementPrinting += Strings.Mid(RSreader["Narration"].ToString(), 1, 15) ;
                        strStatementPrinting += Strings.Format(Conversion.Val(amount), "##,###.00") ;
                        strStatementPrinting += RSreader["DRCR"].ToString();
                        strStatementPrinting += "#";
                        i += 1;
                    }
                    // no datat
                }
                else
                {
                    strResponse += " No data " + "#";
                }
                RSreader.Close();
                strResponse += strStatementPrinting;
                strResponse += "--------------------------------" + "#";

                SQL = "select sum(Amount) as Totalcredits from VW_TELLERAGENT_TRX  ";
                SQL = SQL + "where DRCR='C' AND Channel='POS' AND CreatedBy='" + strTellerAgent + "' AND TrxDate='" + strdate + "'";
                SqlDataReader RSDT = Clogic.RunQueryReturnDataReader(SQL);
                string strTotalcredit = "";
                if (RSDT.HasRows)
                {
                    RSDT.Read();
                    strTotalcredit = RSDT["Totalcredits"].ToString();
                }
                else
                {
                    strTotalcredit = "0.00";
                }
                SQL = "select sum(Amount) as Totaldebits from VW_TELLERAGENT_TRX  ";
                SQL = SQL + "where DRCR='D' AND  Channel='POS' AND CreatedBy='" + strTellerAgent + "' AND TrxDate='" + strdate + "'";

                SqlDataReader RSCT = Clogic.RunQueryReturnDataReader(SQL);
                string strTotaldebits = "";
                if (RSCT.HasRows)
                {
                    RSCT.Read();
                    strTotaldebits = RSCT["Totaldebits"].ToString();
                }
                else
                {
                    strTotaldebits = "0.00";
                }
                // append te totalcredits and totaldebits
                strResponse += "Total Credits.    " + Strings.Format(Conversion.Val(strTotalcredit), "##,###.00") + "#";
                strResponse += "Total Debits.     " + Strings.Format(Conversion.Val(strTotaldebits), "##,###.00") + "#";
                //fetch the account Balance
                string StrMerchantFloatAccount = "";
                if (strTellerAgent.StartsWith("A"))
                {
                    StrMerchantFloatAccount = fn_getAgentAccountNumber(strTellerAgent);
                }
                else if (strTellerAgent.StartsWith("T"))
                {
                    StrMerchantFloatAccount = fn_getAgentAccountNumber(strTellerAgent);
                }
                //fetch the account balance
                string MerchantAccountBalance = null;
                MerchantAccountBalance = fn_GetAccountBalance(StrMerchantFloatAccount);
                strResponse += "Account Bal.      " + Strings.Format(Conversion.Val(MerchantAccountBalance), "##,###.00") + "#";
                strResponse += "--------------------------------" + "#";

                return strResponse;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public string fn_GetAccountBalance(string accountno)
        {
            SqlCommand Command = default(SqlCommand);
            SqlDataAdapter adapter = default(SqlDataAdapter);
            DataTable dt = default(DataTable);
            string AccountBalance = "";

            try
            {
                SqlConnection ConnStoredProcedures = new SqlConnection(Clogic.strConnectionString_);
                if (ConnStoredProcedures.State == ConnectionState.Closed)
                {
                    ConnStoredProcedures.Open();
                }
                Command = new SqlCommand("SP_GETBALANCE", ConnStoredProcedures);
                Command.CommandType = CommandType.StoredProcedure;
                Command.Parameters.AddWithValue("trn_account_number", accountno);
                adapter = new SqlDataAdapter(Command);
                dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    AccountBalance = dt.Rows[0]["AvailableBal"].ToString();
                }
                return (AccountBalance);
            }
            catch (Exception ex)
            {
                return "";
            }

        }

        public void fn_updatetbPOSTransactions(string strField39, string strField48, string strField80, string strField54, string strField127, string myguid, string strResponse, string strField37)
        {
            try
            {
                string strInTransactions = "update tbIncomingPosTransactions set field_39='" + strField39 + "', field_48='" + strField48 + "', field_80='" + strField80 + "', field_54='" + strField54 + "',field_127='" + strField127 + "', " + "field_56='" + myguid.ToString() + "', pos_receipt='" + strResponse + "',response_time='" + Strings.Format(DateAndTime.Now, "dd-MMM-yyyy HH:mm:ss.fff") + "'  where field_0='" + strField37 + "'";

                bool blninupdate = Clogic.RunNonQuery(strInTransactions);

            }
            catch (Exception ex)
            {

            }
        }
        public string fn_getAgentAccountNumber(string strAgentID)
        {
            string strLogin = "";

            try
            {
                if (strAgentID.StartsWith("A"))
                {
                    strLogin = "Select account from VW_AGENTDETAILS where AgentNo ='" + strAgentID + "'";
                }
                else if (strAgentID.StartsWith("T"))
                {
                    strLogin = "Select account from VW_AGENTDETAILS where AgentNo ='" + strAgentID + "'";
                }

                string rsReader = Clogic.RunStringReturnStringValue(strLogin);
                return rsReader;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public string Getminrerno(string field37,string strAgentid)
        {
            string minrefno = "";
            string SQL = "";
            try
            {

               // and Channel='pos' and CreatedBy=''
                SQL = "Select TrxRefNo from VW_TRANSACTION where Field37 ='" + field37 + "' and Channel='POS' and CreatedBy='" + strAgentid +"'" ;
                minrefno = Clogic.RunStringReturnStringValue(SQL);
                return minrefno;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public string GetSchoolname(string Accountnumber)
        {
            string schooldescription = "";
            string SQL = "";

            try
            {
                
                SQL = "Select Description from tbSchools where AccountNo ='" + Accountnumber + "'";
                schooldescription = Clogic.RunStringReturnStringValue(SQL);
                return schooldescription;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public string Verifystudentdetails(string schoolid, string studentnumber)
        {
            string studentname = "";
            string SQL = "";

            try
            {
                SQL = "Select UPPER(FullNames) FullNames from tbStudents where StudentNumber ='" + studentnumber + "' and schoolid='" + schoolid + "'";
                studentname = Clogic.RunStringReturnStringValue(SQL);
                return studentname;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public string GetShooldetails(string strAccountNumber)
        {
            string schoolid = "";
            string SQL = "";

            try
            {
                SQL = "Select SchoolNo from tbSchools where AccountNo ='" + strAccountNumber + "'";
                schoolid = Clogic.RunStringReturnStringValue(SQL);
                return schoolid;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        //get fn_getRefAccount
        public string GetRefAccount(string refnumber)
        {
            string strAccountNumber="";

            try
            {
                DataSet ds = new DataSet();
                string strresponse = "0";
                SqlConnection Conn_minicbs = new SqlConnection(Clogic.strConnectionString_);
                if (Conn_minicbs.State == ConnectionState.Closed)
                {
                    Conn_minicbs.Open();
                }

                SqlCommand cmdInsert = new SqlCommand();
                cmdInsert.Connection = Conn_minicbs;

                cmdInsert.CommandText = "SELECT  dbo.fn_getRefAccount('" + refnumber + "')";
                cmdInsert.CommandType = CommandType.Text;

                strAccountNumber = cmdInsert.ExecuteScalar().ToString();
               
                return strAccountNumber;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
    }
}
