using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace EconnectTMSservice
{
    class ClsConfirmDeposit
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
            string str514narration="";

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');
            try
            {
                 strAmount = strReceivedData[3];
                    strDeviceid = strReceivedData[2];
                   //// strDeviceid = strDeviceid.Substring(0, 15);
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

                    
                    amount = Convert.ToDouble(strAmount);
                   
                    if(amount < 0 )
                    {

                        strResponse = opps.strResponseHeader(strDeviceid);
                        strResponse += "--------------------------------" + "#";
                        strResponse += "Amount must be greater than Zero #";
                        strResponse += opps.strResponseFooter();
                        main.SendPOSResponse(strResponse, MessageGUID);
                        return;
                    }

                    string strAgentCash  = strReceivedData[4];
                    strAgentCash = opps.fn_RemoveNon_Numeric(strAgentCash);
                    string strAgentPwd  = strReceivedData[5];
                    string strfield_48 = "";
                    string StrSupervisorCallNumber  = "";
                    double cashrequestamount  = 0;
                    string strResponse1  = "";

                    StrSupervisorCallNumber = strReceivedData[7].Trim();

                    string callnumberstatus  = opps.GetCallnumberStatus(StrSupervisorCallNumber, Operation.Confirm_Deposit.ToString());

                    if(callnumberstatus != "ok")
                    {
                        strResponse = opps.strResponseHeader(strDeviceid);
                        strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                        strResponse += "--------------------------------" + "#";
                        strResponse += "   Deposit Confirmation      " + "#";
                        strResponse += "--------------------------------" + "#";
                        strResponse += callnumberstatus + "#";
                        strResponse += "" + "#";
                        strResponse += opps.strResponseFooter();
                        main.SendPOSResponse(strResponse, MessageGUID);
                        return;
                    }

                    cashrequestamount = opps.GetCashpickupCallnumber(StrSupervisorCallNumber);

                    if(cashrequestamount == 0 )
                    {
                        strResponse = opps.strResponseHeader(strDeviceid);
                        strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                        strResponse += "--------------------------------" + "#";
                        strResponse += "   Deposit Confirmation      " + "#";
                        strResponse += "--------------------------------" + "#";
                        strResponse += "No CallNumber Exists For: " + StrSupervisorCallNumber + "#";
                        strResponse += "" + "#";
                        strResponse += opps.strResponseFooter();
                        main.SendPOSResponse(strResponse, MessageGUID);
                        return;
                    }

                         Boolean Exist = false;
                    ///'check if the login details of the supervisor exists
                    //'supervisors id's start with S so padd the strAgentCashwith an  S at the BEGINNING
                    Exist = opps.fn_get_Supervisor("S" + strAgentCash, strAgentPwd);
                    Boolean AmountStatus  = false;


                   // 'If strStatus = "Active" Then
                    //'confirm the amount entered by the agent against supervisor amount
                   if(cashrequestamount != amount )
                {
                        strResponse1 += "   Amount Entered differs" + "#";
                        strResponse1 += "   S" + strAgentCash + "  :" + cashrequestamount + "#";
                        strResponse1 += " " + strAgentID + "  :" + strAmount + "#";
                        strField39 = "99";
                        strfield_48 = "Amount Differs";
                        AmountStatus = true;
                   }
                    else
                   {
                        AmountStatus = false;
                   }

                    if( Exist = true && cashrequestamount == amount)
                    {
                        strResponse1 += "I " + "S" + strAgentCash + " have Received " + strAmount + "#";
                        strResponse1 += "         from " + strAgentID + "#";
                        strField39 = "00";
                        strfield_48 = "Successful";
                        AmountStatus = false;
                        //'build the response printout after  econnect response since we can't build two connectionto the POS
                        //'note this is wat the receipt should print upon a response from econnect
                        string agentName  = opps.fn_getAgentName(strAgentID);
                        string supervisorname = opps.fn_get_SupervisorName("S" + strAgentCash);
                        string agentlocation  = opps.fn_getAgentLocation(strAgentID);

                        str514narration += " I " + supervisorname + "," + "#";
                        str514narration += " Sup. No: " + " S" + strAgentCash + "#";
                        str514narration += " confirm cash receipt of " + strAmount + "#";
                        str514narration += " from " + agentName + " Agent No: " + "#";
                        str514narration += " " + strAgentID + " at location :" + "#";
                        str514narration += " " + agentlocation + "#";

                    }
                    else if (Exist == false)
                    {
                        strResponse1 += "Invalid Login Details" + "#";
                        strResponse1 += "Login ID " + strAgentCash + "#";
                        strField39 = "99";
                        strfield_48 = "Login ID or Password Invalid";
                        AmountStatus = true;
                    }

                   // ' fetch the supervisor float account
                    string SupervisorFloat  = "";
                    SupervisorFloat =opps.fn_get_SupervisorFloatAccount("S" + strAgentCash);
                    if(String.IsNullOrEmpty(SupervisorFloat))
                    {

                        ///return an error

                    }

                    opps.spInsertPOSTransaction(intid, "0000000000000000", "401000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "REQUEST CASH DEPOSIT " + strAgentID, "", "", strAgentID, "", "", "");

                    String strInMsg = "update tbincomingPosTransactions set field_39='" + strField39 + "',field_48='" + strfield_48 + "',request_time='" + Strings.Format(DateAndTime.Now, "dd-MMM-yyyy HH:mm:ss.fff") + "',request_to_econnect='" + strResponse + "' where field_0='" + intid.PadLeft(12, '0') + "'";
                    Clogic.RunNonQuery(strInMsg);

                   // ' only print here incase of an error
                    if(AmountStatus == true )
                    {
                        strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                        strResponse += "--------------------------------" + "#";
                        strResponse += "   Deposit Confirmation      " + "#";
                        strResponse += "--------------------------------" + "#";
                        strResponse += strResponse1;
                        strResponse += "" + "#";
                        strResponse += opps.strResponseFooter();
                        main.SendPOSResponse(strResponse, MessageGUID);
                        return;
                    }
                    
                    //'check if amount differs so dont send crap to econect
                    if(AmountStatus == false)
                    {

                        opps.spInsertPOSTransaction(intid, "0000000000000000", "404000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "REQUEST CASH DEPOSIT TO ECONNECT " + strAgentID, "", "", strAgentID, "", "", "");

                        //'send to econnect
                        field24 = "514"; // means agent has excess cash and request for a deposit 
                        Guid myguid= new Guid(MessageGUID);
                        string strRequest = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "010000", strAmount, field24, strDeviceid, "", StrSupervisorCallNumber, "", strAgentID, SupervisorFloat, "", "POS", strAgentID, ref myguid, "AGENT:" + strAgentID + " CASH DEPOSIT-" + SupervisorFloat, "", "", str514narration);
                        main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);

                    }
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "Confirmdeposit", "Confirmdeposit");
            }
        }
    }

}