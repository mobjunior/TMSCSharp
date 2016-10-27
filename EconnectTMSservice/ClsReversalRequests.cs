using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsReversalRequests
    {
        public void Run(string IncomingMessage, string intid, string MessageGUID, string field37, bool pinmessagesent = false)
        {
            ClsMain main = new ClsMain();
            ClsSharedFunctions opps = new ClsSharedFunctions();
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
            string strAccountNumber2 = "";
            string studentnumber = "";
            string studentname = "";
            string TransactionRefNo = "";
            string Minirefno = "";
            Dictionary<string, string> data = new Dictionary<string, string>();
            ClsEbankingconnections Clogic = new ClsEbankingconnections();

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');

            try
            {

                 strAgencyCashManagement = strReceivedData[1];
                 switch (strAgencyCashManagement)
                 {
                     case "AGENCY":
                         //720000#AGENCY#0000011300031089#9000000589#00007#
                        //720000#AGENCY#0000011300031089#9000000589#00007#
                         TransactionRefNo = strReceivedData[3];
                         strAgentID = strReceivedData[4].Replace("Ù", "");
                        //remove those funny characters from the pos memory thy can be a mess make a transaction fail
                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                        if(strAgentID.Length == 5)
                        {
                            strAgentID = "T" + strAgentID;
                        }
                        else
                        {
                            strAgentID = "A" + strAgentID;
                        }

                        strDeviceid = strReceivedData[2].Replace("Ù", "");
                        //check if TransactionRefNo is empty
                        if (String.IsNullOrEmpty(TransactionRefNo))
                        {
                            strResponse = opps.strResponseHeader(strDeviceid);
                            strResponse += "--------------------------------" + "#";
                            strResponse += "         Reversal Request       " + "#";
                            strResponse += "--------------------------------" + "#";
                            strResponse += "   Reference passed empty       #";
                           // strResponse += "--------------------------------" + "#";                       
                            strResponse += opps.strResponseFooter();
                            main.SendPOSResponse(strResponse, MessageGUID);
                            return;
                        }
                        //verify the passed TransactionRefNo

                        Minirefno = opps.Getminrerno(TransactionRefNo,strAgentID);
                        if (String.IsNullOrEmpty(Minirefno))
                        {
                            strResponse = opps.strResponseHeader(strDeviceid);
                            strResponse += "--------------------------------" + "#";
                            strResponse += "         Reversal Request       " + "#";
                            strResponse += "--------------------------------" + "#";
                            strResponse += "   ReferenceNo Does not Exists  #";
                           // strResponse += "--------------------------------" + "#";
                            strResponse += opps.strResponseFooter();
                            main.SendPOSResponse(strResponse, MessageGUID);
                            return;
                        }

                        opps.spInsertPOSTransaction(intid, "", "720000", "", "", "", "", "", "", "", "", strReceivedData[2], "", "", "", "", "", "", "", strAgentID, "", "", "");
                        //all is weell insert into table
                         data["REF_NO"] = Minirefno;
                         data["Channel"] = "POS"  ;
                         data["Approved"] = "0";
                         data["CreatedBy"] = strAgentID;

                         string sql = Clogic.InsertString("tbRvsl_Request", data);
                         if (Clogic.RunNonQuery(sql))
                         {
                             //send an sms to the headteller
                             string PhoneNumber = Clogic.RunStringReturnStringValue("select PhoneNumber from tbTellerUsers where TellerType='006'");
                             string username = Clogic.RunStringReturnStringValue("select UserName from tbTellerUsers where TellerType='006'");
                             string HeadTellerID = Clogic.RunStringReturnStringValue("SELECT TellerId FROM tbTellerUsers WHERE TellerType='006'");

                             string strmessage = "";
                             strmessage = "Dear " + username + ", please process " + strAgentID + " Reversal request. Ebank Reference No. " + Minirefno ;
                             //send sms
                             opps.SendSMS(PhoneNumber, strmessage, "POS", intid, "");


                             strResponse = opps.strResponseHeader(strDeviceid);
                             strResponse += "--------------------------------" + "#";                            
                             strResponse += "         Reversal Request       " + "#";
                             strResponse += "--------------------------------" + "#";
                             strResponse += "   Reversal Request Successful  #";
                             //strResponse += "--------------------------------" + "#"; 
                             strResponse += opps.strResponseFooter();
                             main.SendPOSResponse(strResponse, MessageGUID);
                             //update response
                             opps.fn_updatetbPOSTransactions("00", "Successful", "", "", "", MessageGUID, strResponse, intid);
                             return;
                         }
                         else
                         {
                             strResponse = opps.strResponseHeader(strDeviceid);
                             strResponse += "--------------------------------" + "#";
                             strResponse += "         Reversal Request       " + "#";
                             strResponse += "--------------------------------" + "#";
                             strResponse += "       Request Failed           #";
                             //strResponse += "--------------------------------" + "#";                             
                             strResponse += opps.strResponseFooter();
                             main.SendPOSResponse(strResponse, MessageGUID);
                             //update response
                             opps.fn_updatetbPOSTransactions("05", "Request Failed", "", "", "", MessageGUID, strResponse, intid);
                             return;
                         }

                         break;
                 }
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "clsREversalrequests", "clsREversalrequests");
            }
        }
    }
}
