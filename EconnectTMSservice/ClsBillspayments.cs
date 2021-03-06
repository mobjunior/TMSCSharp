﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsBillspayments
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
            string strBillNumber="";
            string strwalkingcustomerphonenumber = "";
            string[] strReceivedData;
            strReceivedData = IncomingMessage.Split('#');

            try
            {

                 string EcgBalresponse ="";
                    string[] strECGResponse;
                    string strResponseMessage  = "";
                    string status  = "";
                    string customername  = "";
                    string custbal = "";
                    string outstandingdate = "";

                    strAgencyCashManagement = strReceivedData[1];
               

                    switch(strAgencyCashManagement)
                    {
                        case "CASH":

                            strCardNumber = "0000000000000000";
                            strExpiryDate = "0000";
                            strAgentID = strReceivedData[4].Replace("Ù", "");
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                            if(strAgentID.Length == 5)
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "526"; // WALK-IN CUSTOMERS
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
                                strAgentID = "A" + strAgentID;
                                field24 = "512";//WALK-IN CUSTOMERS
                            }
                            strDeviceid = strReceivedData[2].Replace("Ù", "");
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strAccountNumber = strAgentID;
                            strAmount = strReceivedData[3];
                            strBillNumber = strReceivedData[6];
                            strVerifyPin = "00";
                            strField100 = strReceivedData[5];
                            strwalkingcustomerphonenumber = strReceivedData[7];
                           // ''chekc if service is disabled
                            if (strField100 == "SSTV")
                            {
                                strField100 = "STTV";
                            } 
                            if(opps.GetEnabledServices(strField100) == false )
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Billpayment Not Allowed for " + strField100 + "#";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return;
                            }
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
                                if(strlen < 16 )
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
                            else if( strTrack2Data.Contains("D") )
                            {
                                strCardInformation = strTrack2Data.Split('D');
                                strCardNumber = strCardInformation[0];
                                Int32 strlen  = strCardNumber.Length;
                                if(strlen < 16 )
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

                            strAgentID = strReceivedData[7].Trim();
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                            if(strAgentID.Length == 5 )
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "540";
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
                                field24 = "508";
                                strAgentID = "A" + strAgentID;
                            }
                            strAmount = strReceivedData[6];
                            strField100 = strReceivedData[8];
                            ///''chekc if service is disabled
                            ///
                            if (strField100 == "SSTV")
                            {
                                strField100 = "STTV";
                            } 
                            if(opps.GetEnabledServices(strField100) == false)
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Billpayment Not Allowed for " + strField100 + "#";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse,MessageGUID);
                              
                                return;
                            }
                            strBillNumber = strReceivedData[9];
                           
                            //'strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" & strCardNumber & "'")

                            strPinClear = strReceivedData[5].Replace("Ù", "");
                            strPinClear = strPinClear.Substring(0, 4);

                            if (pinmessagesent == false)
                            {
                                EconnectTMSservice.ClsMain.messagesfrompos.Add(intid, IncomingMessage + "|" + MessageGUID + "|" + strDeviceid);
                                strVerifyPin = main.PIN_Verify("500000", strPinClear, strCardNumber, strExpiryDate, strDeviceid, strField35, intid.PadLeft(12, '0'));
                                //opps.spInsertPOSTransaction(intid, strCardNumber, "500000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", strBillNumber, "BILL PAYMENTS " + strBillNumber, "", strField100, strAgentID, strAccountNumber, "", "");
                                
                                return;
                            }
                            else
                            {
                                strField37 = field37 ;
                                strVerifyPin = "00";
                                //for now let the account number from econeect until the switch is able to respond to us
                                //uncomment the 2n line below
                               // strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" + strCardNumber + "'");
                                strAccountNumber = opps.GETACOUNTFROMECONNECT(strCardNumber, strField37);

                            }
                    break;
                    }
                    Guid myguid = new Guid(MessageGUID);

                    opps.spInsertPOSTransaction(intid, strCardNumber, "500000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", strBillNumber,"", "BILL PAYMENTS " + strBillNumber, "", strField100, strAgentID, strAccountNumber, "", "");
                    string strRequest = opps.GenerateXMLtoeConnect(field37, "0200", strCardNumber, "500000", strAmount, field24, strDeviceid, strBillNumber, "", strField100, strAgentID, strAccountNumber, "", "POS", strAgentID, ref myguid, "BILL PAYMENTS " + strBillNumber, strwalkingcustomerphonenumber, "", "", "");
                    main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "Billspayment", "Billspayment");
            }
        }
    }
}
