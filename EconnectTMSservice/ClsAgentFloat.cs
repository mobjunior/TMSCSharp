using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsAgentFloat
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
            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');

            try
            {
                strAgencyCashManagement = strReceivedData[1];
                switch(strAgencyCashManagement)
                {
                    case "CASH":
                        break;

                    case "AGENCY":

                            strCardNumber = "0000000000000000";
                            strExpiryDate = "0000";
                            //'craz POS can send anthing make sure it is numeric for teller or agent id
                            strAgentID = strReceivedData[5].Substring(0, 4).Replace("Ù", "");
                            //'strAgentID = "A" & strReceivedData(5).Substring(0, 4).Replace("Ù", "")
                            strAgentID = "A" + opps.fn_RemoveNon_Numeric(strAgentID);
                            strResponse = opps.strResponseHeader(strReceivedData[2]).Replace("Ù", "");
                            strDeviceid = strReceivedData[2].Replace("Ù", "");
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            field24 = "501";
                            strAccountNumber = "";
                            break;
                }//end of switch case
                Guid myguid =new Guid(MessageGUID);
                opps.spInsertPOSTransaction(intid, strCardNumber, "320000", "0", "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "BALANCE-" + strAgentID, "", "", strAgentID, strAccountNumber, "", "");
                string strRequest = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "320000", "0", field24, strDeviceid, "", "", "", strAgentID, strAccountNumber, "", "POS", strAgentID, ref myguid, "BALANCE-" + strAgentID);
                main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);
                return;

            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "clsagentfloat", "clsagentfloat");
            }
        }
    }
}
