using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsCashRequests
    {
        public void Run(string[] strReceivedData, string intid, string MessageGUID, string field37, bool pinmessagesent = false)
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

            try
            {
                strAmount = strReceivedData[3];
                    strDeviceid = strReceivedData[2];
                    strDeviceid = strDeviceid.Substring(0, 15);
                    strAgentID = strReceivedData[6].Trim();
                    strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                    if(strAgentID.Length == 5)
                    {

                        strAgentID = "T" + strAgentID;
                    }
                    else
                    {
                        strAgentID = "A" + strAgentID;
                    }

                    string strAgentCash = strReceivedData[4];
                    string strAgentPwd  = strReceivedData[5];
                    string strfield_48  = ""
                    //' Do a check if the Agent Details Exists then
                   // ' Check if supervisor exists
                   // ' Dim strStatus As String = Clogic.RunStringReturnStringValue("select status from tbAgentRegistration where agentno like '%" & strAgentCash & "%' and password='" & strAgentPwd & "'")
                    //' Check if supervisor exists
                    Boolean Exist  = false;
                    //'supervisors id's start with S so padd the strAgentCashwith an  S at the BEGINNING
                    Exist = opps.fn_get_Supervisor("S" + strAgentCash, strAgentPwd);

                    string strResponse1  = "";
                    //' If strStatus = "Active" Then
                    string agentName  = opps.fn_getAgentName(strAgentID);
                    string supervisorname  = opps.fn_get_SupervisorName("S" + strAgentCash);

                    if(Exist == true)
                    {
                        strResponse1 += " I " + agentName + "," + "#";
                        strResponse1 += " Agent No: " + strAgentID + "#";
                        strResponse1 += " confirm cash receipt of " + strAmount + "#";
                        strResponse1 += " from " + supervisorname + " Sup. No: " + "#";
                        strResponse1 += " S" + strAgentCash + "#";                       
                        strField39 = "00";
                        strfield_48 = "Successful";
                    }
                    else
                    {
                        strResponse1 += "Invalid Login Details" + "#";
                        strResponse1 += "Login ID " + strAgentCash + "#";
                        strField39 = "99";
                        strfield_48 = "Login ID or Password Invalid";
                    }

                    strResponse = opps.strResponseHeader(strReceivedData[2]);
                    opps.spInsertPOSTransaction(intid, "0000000000000000", "403000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "ACCEPT CASH " + strAgentCash, "", "", strAgentID, "", "", "");

                    string strInMsg  = "update tbincomingPosTransactions set field_39='" + strField39 + "',field_48='" + strfield_48 + "' where field_0='" + intid.PadLeft(12, '0') + "'";
                    Clogic.RunNonQuery(strInMsg);

                    strResponse += "Auth ID:        " + intid.PadLeft(12, '0') + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += "        Cash Acceptance       " + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += strResponse1;
                    strResponse += opps.strResponseFooter();
                                  
                    main.SendPOSResponse(strResponse, MessageGUID);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
