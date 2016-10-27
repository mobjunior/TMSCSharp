using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsAccountlinking
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
            string referencenumber = "";
            string Accountnumber = "";
            string Field102 = "";
            string Field103 = "";

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');

            try
            {
                referencenumber = strReceivedData[3].Replace("Ù", "");
                Accountnumber = strReceivedData[4].Replace("Ù", "");

                Field103 = Accountnumber;

                if (String.IsNullOrEmpty(referencenumber) || String.IsNullOrEmpty(Accountnumber))
                {
                    strResponse = opps.strResponseHeader(strReceivedData[2]);
                    strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += "   Request for Accont Linking   " + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += " `  Some paramenters missing    " + "#";
                    strResponse += "    Kindly Try Again " + "#";
                    strResponse += opps.strResponseFooter();
                    main.SendPOSResponse(strResponse, MessageGUID);
                    return;
                }

                strAgentID = strReceivedData[5].Replace("Ù", "").Trim();
                strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                if (strAgentID.Length == 5)
                {
                    strAgentID = "T" + strAgentID;
                    field24 = "531";
                }
                else
                {
                    field24 = "531";
                    strAgentID = "A" + strAgentID;
                }
                //fetch the account numer from
                Field102 = opps.GetRefAccount(referencenumber);
                //getthe current available bal
                strAmount = opps.fn_GetAccountBalance(Field102);
                //create transaction to econnect as an FT passed to Ebank for transfer of funds from old account to new account
                opps.spInsertPOSTransaction(intid, strCardNumber, "400000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "ACCOUNT LINKING FROM " + Field102 + "TO " + Field103, "", "", strAgentID, Field102,Field103, "");
                //send the transaction to econnect
                ///send to econnect
                Guid myguid = new Guid(MessageGUID);

                string strRequest = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "400000", strAmount, field24, strDeviceid, "", "", "", strAgentID, Field102, Field103, "POS", strAgentID, ref myguid, "ACCOUNT LINKING FROM " + Field102 + "TO " + Field103);
                main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);

            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "AccountLinking", "AccountLinking");
            }

        }
    }
}
