using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsCardlessFulfilment
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
            string strRemittanceCode="";

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');
            try
            {
                 strAgencyCashManagement = strReceivedData[1];
                 switch (strAgencyCashManagement)
                 {
                     case "CASH":

                         strCardNumber = "0000000000000000";
                         strExpiryDate = "0000";
                         strAgentID = strReceivedData[6].Replace("Ù", "").Trim();
                         strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                         strResponse = opps.strResponseHeader(strReceivedData[2]).Replace("Ù", "");
                         strDeviceid = strReceivedData[2].Replace("Ù", "");
                        //// strDeviceid = strDeviceid.Substring(0, 15);
                         strRemittanceCode = strReceivedData[3].Replace("Ù", "");
                         strAmount = strReceivedData[4];
                         strPhoneNumber = strReceivedData[5];

                         if (strAgentID.Length == 5)
                         {
                             strAgentID = "T" + strAgentID;
                             field24 = "505"; 
                             //check if till is open
                             bool Tillopen = false;
                             Tillopen = opps.GetTellerTillstatus(strAgentID);
                             if (Tillopen == false)
                             {
                                 main.Tillnotoperesponse(strAgentID, MessageGUID, strDeviceid, intid);
                                 return;
                             }
                         }
                         else
                         {
                             field24 = "505";
                             strAgentID = "A" + strAgentID;
                         }
                         break;
                     case "AGENCY":

                         strResponse = opps.strResponseHeader(strReceivedData[2]);
                         strCardNumber = "0000000000000000";// 'strReceivedData(3).Substring(0, 16)
                         strDeviceid = strReceivedData[2];
                        //// strDeviceid = strDeviceid.Substring(0, 15);
                         strExpiryDate = "0000";//' strReceivedData(4).Substring(0, 4)
                         strAgentID = strReceivedData[6].Trim();
                         strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                         if (strAgentID.Length == 5)
                         {
                             strAgentID = "T" + strAgentID;
                             field24 = "522";
                             //check if till is open
                             bool Tillopen = false;
                             Tillopen = opps.GetTellerTillstatus(strAgentID);
                             if (Tillopen == false)
                             {
                                 main.Tillnotoperesponse(strAgentID, MessageGUID, strDeviceid, intid);
                                 return;
                             }
                         }
                         else
                         {
                             field24 = "505";
                             strAgentID = "A" + strAgentID;
                         }

                         strAmount = strReceivedData[4];
                         strField100 = strReceivedData[7].ToUpper();
                         strPhoneNumber = strReceivedData[5];
                         strRemittanceCode = strReceivedData[3].Replace("Ù", "");
                     //strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" & strCardNumber & "'")
                         break;
                 }//end of switch

                 Guid myguid = new Guid(MessageGUID);
                 opps.spInsertPOSTransaction(intid, strCardNumber, "630000", strAmount, "", "", "", field24, "", "", "", strReceivedData[2], "", "", strPhoneNumber, strRemittanceCode, strField100 + " CARDLESS FULLFILMENT BY " + strPhoneNumber, "", strField100, strAgentID, strAccountNumber, "", "");
                 if (strField100 == "ATLANTIS")
                 {
                      string strRequest  = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "630000", strAmount, field24, strDeviceid, strPhoneNumber, strRemittanceCode, strField100, strAgentID, "", "", "POS", strAgentID,ref myguid, strField100 + " CARDLESS FULFILLMENT BY " + strPhoneNumber);
                      main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid,strCardNumber);
                 }
                else
                {
                    strResponse = opps.strResponseHeader(strDeviceid);
                    strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += "   Cardless Fulfilment          " + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += strField100 + " Not Enabled      " + "#";
                    strResponse += "#--------------------------------#";
                    strResponse += opps.strResponseFooter();
                    main.SendPOSResponse(strResponse, MessageGUID);
                    return;
                }
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "cardless_fulfilment", "cardless_fulfilment");
            }
        }
    }
}
