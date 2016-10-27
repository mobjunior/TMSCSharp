using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsAgentTellerEODTransactions
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
            string strPhoneNumber = "";
            string strField100 = "";
            string strBillNumber = "";
            string strRemittanceCode = "";
            string strAgentPassword = "";
            string strAgentCode = "";

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');

            try
            {
                 strAgentCode  = strReceivedData[3].Replace("Ù", "");
                    strAgentPassword  = strReceivedData[4].Replace("Ù", "");

                    strAgentPassword = opps.fn_RemoveNon_Numeric(strAgentPassword);
                    strCardNumber = "0000000000000000";
                    strDeviceid = strReceivedData[2];
                   //// strDeviceid = strDeviceid.Substring(0, 15);

                    strExpiryDate = "0000";
                    strAgentID = strReceivedData[5].Trim();
                    strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                    if(strAgentID.Length == 5)
                    {
                        strAgentID = "T" + strAgentID;
                    }
                    else
                    {
                        strAgentID = "A" + strAgentID;
                    }

                    
                    opps.spInsertPOSTransaction(intid, "0000000000000000", "999990", "0", "", intid, "", "", "", "", strField39, strReceivedData[2], "", "", "", "", "Change password", "", strAgentID, "", strAgentID, "", "");


                    strResponse = opps.strResponseagenetTeller(strAgentID);
                    strResponse = strResponse + opps.fn_getAgentTellerEODTransactions(strAgentID);
                    strResponse = strResponse + opps.strResponseFooter();



                    main.SendPOSResponse(strResponse, MessageGUID);
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "Clsagenttellereod", "clsagenttellereod");
            }
        }

        //


    }
}
