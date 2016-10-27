using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsChequeDeposit
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
            string strAgentCode = "";
            string strAgentPassword = "";
            ClsEbankingconnections Clogic = new ClsEbankingconnections();
            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');

            try
            {
                strAgencyCashManagement = strReceivedData[1];

                    strCardNumber = "0000000000000000";
                    strExpiryDate = "0000";
                    strAgentID = strReceivedData[6].Replace("Ù", "");
                    strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                    strResponse = opps.strResponseHeader(strReceivedData[2]).Replace("Ù", "");
                    strDeviceid = strReceivedData[2].Replace("Ù", "");
                   //// strDeviceid = strDeviceid.Substring(0, 15);
                    strAccountNumber = strReceivedData[3].Replace("Ù", "");
                    strAmount = strReceivedData[4].Replace("Ù", "");
                    
                    amount = Convert.ToDouble(strAmount);
                    
                    if(amount < 0)
                    {
                        strResponse = opps.strResponseHeader(strDeviceid);
                        strResponse += "--------------------------------" + "#";
                        strResponse += "Amount must be greater than Zero #";
                        strResponse += opps.strResponseFooter();
                       main. SendPOSResponse(strResponse, MessageGUID);
                        return;
                    }
                    field24 = "509";
                Guid myguid= new Guid(MessageGUID);

                  opps.spInsertPOSTransaction(intid, strCardNumber, "240000", strAmount, "", "", "", field24, "", "", "", strReceivedData[2], "", "", "", "", "POS CASH DEPOSIT", "", "", strAgentID, "", strAccountNumber, "");
                  string strRequest = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "240000", strAmount, field24, strDeviceid, "", "", "", strAgentID, strAccountNumber, "", "POS", strAgentID, ref myguid, "POS CHEQUE DEPOSIT");
                  main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);


            }
            catch (Exception ex)
            {

            }
        }
    }
}
