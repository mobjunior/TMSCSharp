using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualBasic;

namespace EconnectTMSservice
{
    class ClsLogin
    {
        ClsEbankingconnections Clogic = new ClsEbankingconnections();

        public void Run(string IncomingMessage, string intid, string MessageGUID)
        {
            string strAgentCode="";
            string strAgentPassword="";
            string strDeviceid="";
            string strResponse = "";
            string strField39 = "";

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');
                ClsSharedFunctions opp= new ClsSharedFunctions();
                ClsMain main = new ClsMain();
            string[] strLoginStatus;
            try
            {


                  strAgentCode  = strReceivedData[2].Replace("Ù", "");
                  strAgentPassword = strReceivedData[3].Replace("Ù", "");           
                  strLoginStatus = fn_verify_login_details(strAgentCode, strAgentPassword).Split('|');
                                     
                    string strField48  = "";
                    string strUserType  = strLoginStatus[1];
                    string   strStatus  = strLoginStatus[0];
                    strDeviceid = strReceivedData[1].Replace("Ù", "");
                   //// strDeviceid = strDeviceid.Substring(0, 15);
                    strAgentCode =opp.fn_RemoveNon_Numeric(strAgentCode);
                    
                switch(strStatus)
                {
                    case "True":                        
                            switch(strUserType)
                            {
                                case "1":
                                    strResponse = "11#";
                                    strField39 = "00";
                                    strField48 = "Successful";
                                    break;
                                case "2":
                                    strResponse = "12#";
                                    strField39 = "00";
                                    strField48 = "Successful";
                                    break;
                                case "3":
                                    strResponse = "13#";
                                    strField39 = "00";
                                    strField48 = "Successful";
                                        break;
                                case "4":
                                    strResponse = "14#";
                                    strField39 = "00";
                                    strField48 = "Successful";
                                    break;
                                default:
                                    strResponse = "11#";
                                    strField39 = "00";
                                    strField48 = "Successful";
                                    break;
                                }
                            break;
                    case "":
                            strResponse = "00#";
                            strField39 = "99";
                            strField48 = "Login Failed. Agent Status not Found";
                        break;
                    default:
                            strResponse = "00#";
                            strField39 = "99";
                            strField48 = "Login Failed. Confirm if Agent Exists and If Active";
                        break;
                    }

                if (fn_terminal_allowed(strDeviceid)== false )
                {
                        strResponse = "01#";
                        strField39 = "99";
                        strField48 = "Terminal Blocked. Please Contact Bank.";
                }

                    if(strAgentCode.Trim().Length == 5)
                    {
                        strAgentCode = "T" + strAgentCode;
                    }else
                    {
                        strAgentCode = "A" + strAgentCode;
                    }

                    opp.spInsertPOSTransaction(intid, "0000000000000000", "000000", "0", "", intid, "", "", "", "", strField39, strReceivedData[1], "", "", "", "", "System Login", "", strAgentCode, "", strAgentCode, "", "");

                    string strInMsg  = "update tbincomingPosTransactions set field_48='" + strField48 + "' where field_0='" + intid + "'";
                    Clogic.RunNonQuery(strInMsg);
 
                //send response to pos
                    main.SendPOSResponse(strResponse, MessageGUID);

            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "ClsLogin", "Clslogin");
            }
        }

        //
        private string fn_verify_login_details(string strUsername, string strPassword)
        {
            try
            {
                string strStatus = "";
                string strUserType = "";

                if (strUsername.Length == 5)
                {
                    strUsername = "T" + strUsername;
                }
                else
                {
                    strUsername = "A" + strUsername;
                }
                //we be passing encrpted passwed
                MySecurity.CryptoFactory CrytographFactory = new MySecurity.CryptoFactory();
                MySecurity.ICrypto Crytographer = CrytographFactory.MakeCryptographer();

                string MyPassword = Crytographer.Encrypt(strPassword);
                //Dim MyPassword As String = strPassword

                string strLogin = "Select * from VW_AGENTDETAILS where AgentNo ='" + strUsername + "' and password='" + MyPassword + "'";
                SqlDataReader rsReader = Clogic.RunQueryReturnDataReader(strLogin);
                if (rsReader.HasRows)
                {
                    while (rsReader.Read())
                    {
                        strStatus = rsReader["Active"].ToString() ;
                        strUserType = rsReader["UserType"].ToString();
                    }
                    return strStatus + "|" + strUserType;
                }
                else
                {
                    strStatus = "False";
                    strUserType = "0";
                    return strStatus + "|" + strUserType;
                }

            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "fn_verify_login_Details", "fn_verify_login_Details");
                return "";
            }
        }
        //

        private string fn_verify_Teller_login_details(string strUsername, string strPassword)
        {
            try
            {
                string strStatus = "";
                string strUserType = "";

                if (strUsername.Length == 5)
                {
                    strUsername = "T" + strUsername;
                }
                else
                {
                    strUsername = "A" + strUsername;
                }
                //we be passing encrpted passwed
                MySecurity.CryptoFactory CrytographFactory = new MySecurity.CryptoFactory();
                MySecurity.ICrypto Crytographer = CrytographFactory.MakeCryptographer();

                string MyPassword = Crytographer.Encrypt(strPassword);
                //Dim MyPassword As String = strPassword

                string strLogin = "Select * from tbTellerUsers where TellerId ='" + strUsername + "' and password='" + MyPassword + "'";
                SqlDataReader rsReader = Clogic.RunQueryReturnDataReader(strLogin);
                if (rsReader.HasRows)
                {
                    while (rsReader.Read())
                    {
                        strStatus = rsReader["Active"].ToString();
                        strUserType = rsReader["UserType"].ToString();
                    }
                    return strStatus + "|" + strUserType;
                }
                else
                {
                    strStatus = "False";
                    strUserType = "0";
                    return strStatus + "|" + strUserType;
                }

            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "fn_verify_login_Details", "fn_verify_login_Details");
                return "";
            }
        }
        //
        private bool fn_terminal_allowed(string strTerminalSerialNumber)
        {
            try
            {
                //Dim blnActive As String = Clogic.RunStringReturnStringValue("select active from tbPOSDevices where POSSerial='" & strTerminalSerialNumber & "'")
                string blnActive = "0";

                if(strTerminalSerialNumber.Length >= 15)
                  blnActive = Clogic.RunStringReturnStringValue("select active from tbPOSDevices where left(POSSerial,15) ='" + strTerminalSerialNumber.Substring(0, 15) + "'");
                else
                    blnActive = Clogic.RunStringReturnStringValue("select active from tbPOSDevices where POSSerial ='" + strTerminalSerialNumber + "'");
                
                switch (blnActive)
                {
                    case "True":
                    case "1":
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
