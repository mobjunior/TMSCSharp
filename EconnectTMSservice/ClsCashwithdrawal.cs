using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsCashwithdrawal
    {

        public void Run(string IncomingMessage, string intid, string MessageGUID, string field37, bool pinmessagesent = false)
        {
            ClsMain main = new ClsMain();
            ClsSharedFunctions opps = new ClsSharedFunctions();
            string strCardNumber="";
            string strDeviceid="";
            string strExpiryDate="";
            string strAccountNumber="";
            string strResponse ="";
            string strAgentID="";
            string strAmount="";
            double amount;
            string field24 ="";
            string strTrack2Data="";
            string strAgencyCashManagement="";
            string[] strCardInformation;
            string strField35="";
            string strPinClear = "";
            string strVerifyPin = "";
            ClsEbankingconnections Clogic = new ClsEbankingconnections();

            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');
            try
            {
                strAgencyCashManagement = strReceivedData[1];
                switch(strAgencyCashManagement)
                {
                    case "CASH":
                            strCardNumber = "0000000000000000";
                            strDeviceid = strReceivedData[2];
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strExpiryDate = "0000";
                            strAccountNumber = "";
                            strResponse = opps.strResponseHeader(strReceivedData[2]);
                            strAgentID = strReceivedData[7].Replace("Ù", "");
                            strAmount = strReceivedData[6].Replace("Ù", "");
                            
                            amount = Convert.ToDouble(strAmount);

                           
                            if (amount < 0 )
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Amount must be greater than Zero #";
                                strResponse += opps.strResponseFooter(strDeviceid);

                                main.SendPOSResponse(strResponse,MessageGUID);                               
                                return;
                            }
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                            field24 = "502";

                            break;
                    case "AGENCY":
                        strDeviceid = strReceivedData[2];
                            strAgentID = strReceivedData[7].Replace("Ù", "").Trim();
                            strAmount = strReceivedData[6].Replace("Ù", "");
                           
                            amount = Convert.ToDouble(strAmount);
                           
                            if (amount < 0)
                            {

                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Amount must be greater than Zero #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                            }

                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                            strTrack2Data = strReceivedData[3].Replace("Ù", "");
                            strTrack2Data = strReceivedData[3].Replace("?", "");

                           
                            
                            if (strTrack2Data.Contains("="))
                            {
                                
                                strCardInformation = strTrack2Data.Split('=');
                                strCardNumber = strCardInformation[0];
                                
                                Int32 strlen = strCardNumber.Length;
                                if( strlen < 16 )
                                {
                                    strResponse = opps.strResponseHeader(strDeviceid);
                                    strResponse += "--------------------------------" + "#";
                                    strResponse += "Invalid PAN #";
                                    strResponse += opps.strResponseFooter();
                                    main.SendPOSResponse(strResponse, MessageGUID);
                                    return ;
                                }

                                strExpiryDate = strCardInformation[1].Substring(0, 4);
                                string[] strTrack2Data1 = strCardInformation[1].Split('?');
                                strField35 = strCardInformation[0] + "=" + strTrack2Data1[0].Substring(0, 7);
                            }
                            else if(strTrack2Data.Contains("D"))
                            {
                                strCardInformation = strTrack2Data.Split('D');
                                strCardNumber = strCardInformation[0];
                                Int32 strlen = strCardNumber.Length;
                                if( strlen < 16)
                                {
                                    strResponse = opps.strResponseHeader(strDeviceid);
                                    strResponse += "--------------------------------" + "#";
                                    strResponse += "INVALID PAN #";
                                    strResponse += opps.strResponseFooter(strDeviceid);
                                    main.SendPOSResponse(strResponse, MessageGUID);
                                    return;
                                }

                                strExpiryDate = strCardInformation[1].Substring(0, 4);
                                string[] strTrack2Data1 = strCardInformation[1].Split('?');
                                strField35 = strCardInformation[0] + "=" + strTrack2Data1[0].Substring(0, 7);
                            }

                            if(strAgentID.Trim().Length == 5)
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "521";
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
                                field24 = "502";
                                strAgentID = "A" + strAgentID;
                            }

                          //   strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" & strCardNumber & "'")

                            if (pinmessagesent == false) //pin not verified so we exit after queing into the queue
                            {
                                strPinClear = strReceivedData[5].Replace("Ù", "");
                                strPinClear = strPinClear.Substring(0, 4);

                                EconnectTMSservice.ClsMain.messagesfrompos.Add(intid, IncomingMessage + "|" + MessageGUID.ToString() + "|" + strDeviceid);

                                strVerifyPin = main.PIN_Verify("010000", strPinClear, strCardNumber, strExpiryDate, strDeviceid, strField35, intid.PadLeft(12, '0'));
                              
                                //exit here wait for pin results.
                                return;

                            }
                            else
                            {
                                //pin done and successful
                                string strField37 = field37;
                                strVerifyPin = "00";
                                //for now let the account number from econeect until the switch is able to respond to us
                                //uncomment the 2n line below
                               // strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" + strCardNumber + "'");
                                strAccountNumber = opps.GETACOUNTFROMECONNECT(strCardNumber, strField37);
                                opps.spInsertPOSTransaction(intid, strCardNumber, "010000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "POS CASH WITHDRAWAL-" + strAccountNumber, "", "", strAgentID, strAccountNumber, "", "");
                                strField37 = strField37.PadLeft(12, '0');
                                Guid myguid = new Guid(MessageGUID);
                                string strRequest = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "010000", strAmount, field24, strDeviceid, "", "", "", strAgentID, strAccountNumber, "", "POS", strAgentID, ref myguid, "POS CASH WITHDRAWAL-" + strAccountNumber);
                           //send message to econnect for processing
                                main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);
                                return;
                                //wait for reply from econnect on socket arrival for processed transaction

                            }
                         break;
                }// end of switch




            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "Cashwithdrawal", "Cashwithdrawal");
            }
        }
    }
}
