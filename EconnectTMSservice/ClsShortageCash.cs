using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace EconnectTMSservice
{
    class ClsShortageCash
    {
        public void Run(string IncomingMessage, string intid, string MessageGUID, string field37, bool pinmessagesent = false)
        {
            ClsMain main = new ClsMain();
            ClsSharedFunctions opps = new ClsSharedFunctions();
            ClsEbankingconnections Clogic = new ClsEbankingconnections();
            string strCardNumber = "";
            string strDeviceid = "";
            string strExpiryDate = "";
            string strAccountNumber = "";
            string strResponse = "";
            string strAgentID = "";
            string strAmount = "";
            double amount;
            string field24 = "";
            string strTrack2Data = "";
            string strAgencyCashManagement = "";
            string[] strCardInformation;
            string strField35 = "";
            string strPinClear = "";
            string strVerifyPin = "";
            string strNarration = "";
            string strField37 = "";
            string strField39 = "";
            string str514narration = "";


            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');

            try
            {
                 strAmount = strReceivedData[5];
                    strDeviceid = strReceivedData[2];
                   //// strDeviceid = strDeviceid.Substring(0, 15);
                    strAgentID = strReceivedData[6].Trim();
                    strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                    if(strAgentID.Length == 5 )
                    {
                        strAgentID = "T" + strAgentID;
                    }
                    else
                    {
                        strAgentID = "A" + strAgentID;
                     }
                   

                    strField39 = "00";
                    strResponse = opps.strResponseHeader(strReceivedData[2]);

                    opps.spInsertPOSTransaction(intid, "0000000000000000", "406000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "REQUEST FOR SHORTAGE CASH " + strAgentID, "", "", strAgentID, "", "", "");

                    string strInMsg  = "update tbincomingPosTransactions set field_39='00',field_48='Successfull' where field_0='" + intid.PadLeft(12, '0') + "'";
                    Clogic.RunNonQuery(strInMsg);

                    strResponse += "Auth ID:        " + intid.PadLeft(12, '0') + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += "   Request for Shortage Cash     " + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += "Request for " + strAmount + "#";
                    strResponse += "Received Successful" + "#";
                    strResponse += opps.strResponseFooter();

                 

                    main.SendPOSResponse(strResponse, MessageGUID);

                    //' get the teller details
                    string strSqlManagerDetails  = "";
                    string TellerName  = "";
                    string Branch  = "";
                    string strTellerEmail = "";
                    string branchLocation  = "";
                    string strPhoneNumberToAlert="";

                    if(strAgentID.Contains("T"))
                    {
                        strSqlManagerDetails = "select * FROM tbTellerUsers WHERE [TellerID]='" + strAgentID + "'";
                        SqlDataReader rsrecordTeller  = Clogic.RunQueryReturnDataReader(strSqlManagerDetails);
                        if(rsrecordTeller.HasRows)
                        {
                            rsrecordTeller.Read();
                            TellerName = rsrecordTeller["TellerName"].ToString();
                            Branch = rsrecordTeller["Branch"].ToString();
                            strPhoneNumberToAlert = rsrecordTeller["PhoneNumber"].ToString();
                            strTellerEmail = rsrecordTeller["EmailAddress"].ToString();
                            string strSQL1 = "";
                            strSQL1 = "select * from tbBranches where Branchcode='" + Branch + "'";
                            SqlDataReader rsrecordbranch   = Clogic.RunQueryReturnDataReader(strSQL1);
                            if(rsrecordbranch.HasRows)
                            {
                                rsrecordbranch.Read();
                                branchLocation = rsrecordbranch["Location"].ToString();
                            }
                            
                        }

                    }

                    string tbGENERALPARAMS = "";
                    string HeadofAgencyBanking  = "HeadofAgencyBanking";
                    string SystemOperationsManager = "SystemOperationsManager";
                    string strSQL= "";
                    strSQL = "select ITEMNAME,ITEMVALUE FROM tbGENERALPARAMS WHERE ITEMNAME='" + HeadofAgencyBanking + "'";
                    SqlDataReader rsrecordTemp   = Clogic.RunQueryReturnDataReader(strSQL);

                    if(rsrecordTemp.HasRows)
                    {
                        rsrecordTemp.Read();
                        HeadofAgencyBanking = rsrecordTemp["ITEMVALUE"].ToString();
                    }
                    strSQL = "select ITEMNAME,ITEMVALUE FROM tbGENERALPARAMS WHERE ITEMNAME='" + SystemOperationsManager + "'";
                    rsrecordTemp = null;

                    rsrecordTemp = Clogic.RunQueryReturnDataReader(strSQL);
                    if(rsrecordTemp.HasRows )
                    {
                        rsrecordTemp.Read();
                        SystemOperationsManager = rsrecordTemp["ITEMVALUE"].ToString();
                    }
                    string strMsgHead  = "";

                    strMsgHead = "A cash shortage request of "+ sharedvars.Currencycode+": " + strAmount + " from teller no  " + strAgentID + " at location  " + branchLocation + " has been sent for authorization. ";
                    string strInsertMsg  = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " +
                             " values ('" + HeadofAgencyBanking + "','" + intid + "','" + strAgentID + "','" + strMsgHead + "','POS',0,0)";
                    Boolean blninmsg  = Clogic.RunNonQuery(strInsertMsg);

                    string strMsgSysOPMgr = "";
                    strMsgSysOPMgr = "A cash shortage request of "+ sharedvars.Currencycode +": " + strAmount + " from teller no  " + strAgentID + " at location  " + branchLocation + " has been sent for authorization. ";
                    string strInsertMsgsys = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " +
                             " values ('" + SystemOperationsManager + "','" + intid + "','" + strAgentID + "','" + strMsgSysOPMgr + "','POS',0,0)";
                    Boolean blninmsgsys  = Clogic.RunNonQuery(strInsertMsgsys);

                   // 'log the details of teh shortage cash in table tbshortageExcesscash
                    strInMsg = "insert into tbShortageExcessCash (TellerID,TellerName,amount,Branch,TransactionType) values ('" + strAgentID + "','" + TellerName + "','" + strAmount + "','" + branchLocation + "','shortageCash')";
                    blninmsg = Clogic.RunNonQuery(strInMsg);
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "ShortageCash", "ShortageCash");
            }
        }
    }
}
