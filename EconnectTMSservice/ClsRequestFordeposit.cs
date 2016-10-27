using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace EconnectTMSservice
{
    class ClsRequestFordeposit
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

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');

            try
            {
                 strAmount = strReceivedData[3];
                    strDeviceid = strReceivedData[2];
                   //// strDeviceid = strDeviceid.Substring(0, 15);
                    strAgentID = strReceivedData[4].Trim();
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

                    opps.spInsertPOSTransaction(intid, "0000000000000000", "402000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "REQUEST CASH DEPOSIT " + strAgentID, "", "", strAgentID, "", "", "");

                    string strInMsg  = "update tbincomingPosTransactions set field_39='00',field_48='Successfull' where field_0='" + intid.PadLeft(12, '0') + "'";
                    Clogic.RunNonQuery(strInMsg);

                    strResponse += "Auth ID:            " + intid.PadLeft(12, '0') + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += "   Request for Cash Pick up     " + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += "Request for " + strAmount + "#";
                    strResponse += "Received Successful" + "#";
                    strResponse += opps.strResponseFooter();

                    main.SendPOSResponse(strResponse, MessageGUID);

                
                    string strSqlManagerDetails  = "";
                    strSqlManagerDetails = "SELECT tbSupervisors.ManagerID, tbManagers.EmailAddress, tbManagers.MobileNumber, tbManagers.ManagerName " +
                        ",tbAgentRegistration.MobileNo,tbAgentRegistration.Agent,tbAgentRegistration.Address, tbAgentRegistration.FloatAccount " +
                        "FROM tbAgentRegistration INNER JOIN tbSupervisors ON tbAgentRegistration.SupervisorID = tbSupervisors.SupervisorID INNER JOIN" +
                        " tbManagers ON tbSupervisors.ManagerID = tbManagers.ManagerID where tbAgentRegistration.AgentNo='" + strAgentID + "'";

                    SqlDataReader rsrecord = Clogic.RunQueryReturnDataReader(strSqlManagerDetails);
                    string strManagerName = "";
                    string strManagerEmail  = "";
                    string strAgentPhone  = "";
                    string strAgentFloatAccount  = "";
                    string AgentName  = "";
                    string Address  = "";
                    string strPhoneNumberToAlert="";

                    if(rsrecord.HasRows)
                    {
                        while (rsrecord.Read())
                        {
                            strManagerName = rsrecord["ManagerName"].ToString();
                            strManagerEmail = rsrecord["EmailAddress"].ToString();
                            strPhoneNumberToAlert = rsrecord["MobileNumber"].ToString();
                            strAgentPhone = rsrecord["MobileNo"].ToString();
                            strAgentFloatAccount = rsrecord["FloatAccount"].ToString();
                            AgentName = rsrecord["Agent"].ToString();
                            Address = rsrecord["Address"].ToString();
                        }
                    }
                    string strMessage = "Dear " + strManagerName + ", the Agent: " + strAgentID + " has requested for cash pickup" + sharedvars.Currencycode +": " + strAmount + " . Kindly dispatch a supervisor. ";
                    string strInsertMsg  = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " +
                             " values ('" + strPhoneNumberToAlert + "','" + intid + "','" + strAgentID + "','" + strMessage + "','POS',0,0)";
                    Boolean blninmsg  = Clogic.RunNonQuery(strInsertMsg);

                    // send sms to the head of agency banking and system operation manager
                    string HeadofAgencyBanking = "HeadofAgencyBanking";
                    string SystemOperationsManager = "SystemOperationsManager";
                    string strSQL  = "";

                    strSQL = "select ITEMNAME,ITEMVALUE FROM tbGENERALPARAMS WHERE ITEMNAME='" + HeadofAgencyBanking + "'";
                    SqlDataReader rsrecordTemp  = Clogic.RunQueryReturnDataReader(strSQL);

                    if(rsrecordTemp.HasRows)
                    {
                        rsrecordTemp.Read();
                        HeadofAgencyBanking = rsrecordTemp["ITEMVALUE"].ToString(); 
                    }
                    strSQL = "select ITEMNAME,ITEMVALUE FROM tbGENERALPARAMS WHERE ITEMNAME='" + SystemOperationsManager + "'";
                    rsrecordTemp = null;


                    rsrecordTemp = Clogic.RunQueryReturnDataReader(strSQL);
                    if(rsrecordTemp.HasRows)
                    {

                        rsrecordTemp.Read();
                        SystemOperationsManager = rsrecordTemp["ITEMVALUE"].ToString();
                    }

                    string HeadofAgencyBankingName  = "HeadofAgencyBankingName";
                    string SystemOperationsManagerName = "SystemOperationsManagerName";
                    HeadofAgencyBankingName = opps.fn_get_AdministratorNames("HeadofAgencyBankingName");
                    SystemOperationsManagerName = opps.fn_get_AdministratorNames("SystemOperationsManagerName");
                    //' send the sms

                    string strMsgHead = "";
                    strMsgHead = "Dear " + HeadofAgencyBankingName + ", the Agent: " + strAgentID + " has requested for cash pickup " + sharedvars.Currencycode+": " + strAmount + " . Kindly dispatch a supervisor. ";
                    string strInsertMsgHead = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " +
                             " values ('" + HeadofAgencyBanking + "','" + intid + "','" + strAgentID + "','" + strMsgHead + "','POS',0,0)";

                    Boolean blninmsgHead  = Clogic.RunNonQuery(strInsertMsgHead);

                    string strMsgSysOPMgr  = "";
                    strMsgSysOPMgr = "Dear " + SystemOperationsManagerName + ", the Agent: " + strAgentID + " has requested for cash pickup "+ sharedvars.Currencycode+ ":" + strAmount + " . Kindly dispatch a supervisor. ";
                    string strInsertMsgsys  = "insert into tbmessages_sms (PhoneNumber,TransactionNo,AccountNumber,Message,channel,sent,delivered) " +
                             " values ('" + SystemOperationsManager + "','" + intid + "','" + strAgentID + "','" + strMsgSysOPMgr + "','POS',0,0)";
                    Boolean blninmsgsys  = Clogic.RunNonQuery(strInsertMsgsys);

                    

                    strInMsg = "insert into tbMIBRequest (Source,ComplaintName,Resolution,AccountNumber,MobileNumber,amount,ProCode) values ('POS','" + strAgentID + "','AGENT CASH PICKUP REQUEST-" + strAmount + "','" + strAgentFloatAccount + "','" + strAgentPhone + "','" + strAmount + "','402000')";
                    blninmsg = Clogic.RunNonQuery(strInMsg);

            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "RequestFordeposit", "Requestfordeposit");
            }

        }
    }
}
