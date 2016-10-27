using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsLoanRepaymentEbank
    {
        public void Run(string IncomingMessage, string intid, string MessageGUID, string field37, bool pinmessagesent = false)
        {

            try
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
                string strField60 = "";
                ClsEbankingconnections Clogic = new ClsEbankingconnections();
                string[] strReceivedData;
                strReceivedData = IncomingMessage.Split('#');

                 strAgencyCashManagement = strReceivedData[1];
                 switch (strAgencyCashManagement)
                 {
                     case "CASH":
                          strCardNumber = "0000000000000000";
                            strExpiryDate = "0000";

                            strAgentID = strReceivedData[5].Replace("Ù", "");
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                            strDeviceid = strReceivedData[2].Replace("Ù", "");
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strAccountNumber = strReceivedData[3].Replace("Ù", "");
                            strAmount = strReceivedData[4].Replace("Ù", "");

                          if(strAgentID.Length == 5)
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "538";
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
                                field24 = "530";
                                strAgentID = "A" + strAgentID;                               
                            }
                         

                            amount = Convert.ToDouble(strAmount);
                          
                            if( amount < 0)
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Amount must be greater than Zero #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return;
                            }

                            strVerifyPin = "00";
                            strNarration = "LOAN REPAYMENT TO-" + strAccountNumber;

                         break;
                     case "AGENCY":
                          strDeviceid = strReceivedData[2];
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strTrack2Data = strReceivedData[3].Replace("Ù", "");
                            strTrack2Data = strReceivedData[3].Replace("?", "");

                          
                            if(strTrack2Data.Contains("="))
                            {
                                strCardInformation = strTrack2Data.Split('=');
                                strCardNumber = strCardInformation[0];
                                Int32 strlen = strCardNumber.Length;
                                if(strlen < 16)
                                {
                                    strResponse = opps.strResponseHeader(strDeviceid);
                                    strResponse += "--------------------------------" + "#";
                                    strResponse += "INVALID PAN #";
                                    strResponse += opps.strResponseFooter();
                                    main.SendPOSResponse(strResponse, MessageGUID);
                                    return;
                                }
                                strExpiryDate = strCardInformation[1].Substring(0, 4);
                                string[] strTrack2Data1 = strCardInformation[1].Split('?');
                                strField35 = strCardInformation[0] + "=" + strTrack2Data1[0].Substring(0, 7);
                            }
                            else if(strTrack2Data.Contains("D"))
                            {
                                strCardInformation = strTrack2Data.Split('D');
                                strCardNumber = strCardInformation[0];
                                Int32 strlen  = strCardNumber.Length;
                                if(strlen < 16)
                                {
                                    strResponse = opps.strResponseHeader(strDeviceid);
                                    strResponse += "--------------------------------" + "#";
                                    strResponse += "INVALID PAN #";
                                    strResponse += opps.strResponseFooter();
                                    main.SendPOSResponse(strResponse, MessageGUID);
                                    return;
                                }
                                strExpiryDate = strCardInformation[1].Substring(0, 4);
                                string[] strTrack2Data1 = strCardInformation[1].Split('?');
                                strField35 = strCardInformation[0] + "=" + strTrack2Data1[0].Substring(0, 7);
                            }
                            strAgentID = strReceivedData[7];
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                            if(strAgentID.Length == 5)
                        {
                                strAgentID = "T" + strAgentID;
                                field24 = "523";
                                //check if till is open
                                bool Tillopen = false;
                                Tillopen = opps.GetTellerTillstatus(strAgentID);
                                if (Tillopen == false)
                                {
                                    main.Tillnotoperesponse(strAgentID, MessageGUID, strDeviceid, intid);
                                    return;
                                }
                        }
                            else{

                                strAgentID = "A" + strAgentID;
                                field24 = "509";
                            }

                            strAmount = strReceivedData[6];

                            amount = Convert.ToDouble(strAmount);
                           
                            if(amount < 0)
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Amount must be greater than Zero #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return ;
                            }
                        //enable below to skip 
                        //strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" & strCardNumber & "'")
                            strAmount = strReceivedData[6];
                            strPinClear = strReceivedData[5].Replace("Ù", "");
                            strPinClear = strPinClear.Substring(0, 4);
                        if(pinmessagesent == false)
                        {
                            EconnectTMSservice.ClsMain.messagesfrompos.Add(intid, IncomingMessage + "|" + MessageGUID + "|" + strDeviceid);                       
                            strVerifyPin = main.PIN_Verify("530000", strPinClear, strCardNumber, strExpiryDate, strDeviceid, strField35, intid.PadLeft(12, '0'));
                           
                            opps.spInsertPOSTransaction(intid, strCardNumber, "530000", strAmount, "", "", "", field24, "", "", "", strReceivedData[2], "", "", "", "", strNarration, "", "", strAgentID, "", strAccountNumber, "");
                            return;
                        }
                        else
                        {
                            //pin done and successful
                                                     
                            string strField37 = field37;
                            strVerifyPin = "00";
                            //for now let the account number from econeect until the switch is able to respond to us
                            //uncomment the 2n line below
                            strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" + strCardNumber + "'");
                            // strAccountNumber = opps.GETACOUNTFROMECONNECT(strCardNumber, strField37);
                            strNarration = "POS CASH DEPOSIT-" + strAccountNumber;   
                            strField37 = strField37.PadLeft(12, '0');
                            Guid myguid = new Guid(MessageGUID);
                            string strRequest = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "530000", strAmount, field24, strDeviceid, "", "", "", strAgentID, "", strAccountNumber, "POS", strAgentID,ref myguid, strNarration);
                            main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);
                            return;
                            //wait for reply from econnect on socket arrival for processed transaction
                        }
                     break;
                 }//end of switch

                 opps.spInsertPOSTransaction(intid, strCardNumber, "530000", strAmount, "", "", "", field24, "", "", "", strReceivedData[2], "", "", "", "", strNarration, "", "", strAgentID, "", strAccountNumber, "");
                 Guid myguid1 = new Guid(MessageGUID);
                 string strRequesttoEconect = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "530000", strAmount, field24, strDeviceid, "", "", "", strAgentID, "", strAccountNumber, "POS", strAgentID, ref myguid1, strNarration);
                 main.SendToEconnect(strRequesttoEconect, intid, MessageGUID, strDeviceid, strCardNumber);
               
            }
            catch (Exception ex)
            {

            }
        }
    }
}
