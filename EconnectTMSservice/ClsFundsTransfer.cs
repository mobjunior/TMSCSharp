using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsFundsTransfer
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
            string strAccountNumber2="";
            string studentnumber ="";
            string studentname ="";

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
                            strExpiryDate = "0000";
                            strAgentID = strReceivedData[5].Replace("Ù", "");
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                            strResponse = opps.strResponseHeader(strReceivedData[2]).Replace("Ù", "");
                            strDeviceid = strReceivedData[2].Replace("Ù", "");
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strAccountNumber = strReceivedData[3].Replace("Ù", "");
                            strAmount = strReceivedData[4];
                           
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
                            strAgentID = "A1021";
                            field24 = "503";
                            break;
                        case "SCHOOLFEES":
                            //sProcessingCode, strAgencyCashManagement, SerialNo, strField35, strExpiryDateField, strField52Pin, strAmountEntered, strCustomerAccountNumber,strStudentNumber, strAgentCode);
                             strDeviceid = strReceivedData[2];
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strAgentID = strReceivedData[9].Trim();
                           // 'crazy pos can send crazy character so extract em from the good ones
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                            strTrack2Data = strReceivedData[3].Replace("Ù", "");
                            strTrack2Data = strReceivedData[3].Replace("?", "");
                            studentnumber = strReceivedData[8].Replace("?", "");
                            strAccountNumber2 = strReceivedData[7];
                            //verify the shool account number
                            string schoolacc = opps.GetShooldetails(strAccountNumber2);
                            if (String.IsNullOrEmpty(schoolacc))
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "School account does not exists #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return;
                            }
                            //verify student details
                            studentname = opps.Verifystudentdetails(schoolacc, studentnumber);
                            if (String.IsNullOrEmpty(schoolacc))
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "student number does not exists #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return;
                            }
                            if(strTrack2Data.Contains("="))
                            {
                                strCardInformation = strTrack2Data.Split('=');
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
                                string[] strTrack2Data1  = strCardInformation[1].Split('?');
                                strField35 = strCardInformation[0] + "=" + strTrack2Data1[0].Substring(0, 7);
                            }
                            

                            if(strAgentID.Length == 5)
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "535";
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
                                field24 = "535";
                                strAgentID = "A" + strAgentID;
                            }

                            strAmount = strReceivedData[6];
                           
                            amount = Convert.ToDouble(strAmount);
                           
                            if( amount < 0 )
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Amount must be greater than Zero #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return;
                            }
                            
                            // strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" & strCardNumber & "'")

                            strPinClear = strReceivedData[5].Replace("Ù", "");
                            strPinClear = strPinClear.Substring(0, 4);

                            if (pinmessagesent == false)
                            {
                                EconnectTMSservice.ClsMain.messagesfrompos.Add(intid, IncomingMessage + "|" + MessageGUID + "|" + strDeviceid);
                                strVerifyPin = main.PIN_Verify("400000", strPinClear, strCardNumber, strExpiryDate, strDeviceid, strField35, intid.PadLeft(12, '0'));

                                opps.spInsertPOSTransaction(intid, strCardNumber, "400000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "POS FUNDS TRANSFER TO " + strAccountNumber2 + " " + studentname + " " + studentnumber, "", "", strAgentID, strAccountNumber, strAccountNumber2, "");
                                

                                return;
                            }
                            else
                            {
                                //pin
                                strField37 = field37;
                                strVerifyPin = "00";
                                //for now let the account number from econeect until the switch is able to respond to us
                                //uncomment the 2n line below
                               // strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" + strCardNumber + "'");
                                strAccountNumber = opps.GETACOUNTFROMECONNECT(strCardNumber, strField37);
                                strNarration = "POS FUNDS TRANSFER TO " + strAccountNumber2 + " " + studentname + " " + studentnumber;
                            }
                            break;
                        case "AGENCY":

                            strDeviceid = strReceivedData[2];
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strAgentID = strReceivedData[8].Trim();
                           // 'crazy pos can send crazy character so extract em from the good ones
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                            strTrack2Data = strReceivedData[3].Replace("Ù", "");
                            strTrack2Data = strReceivedData[3].Replace("?", "");
                           
                            if(strTrack2Data.Contains("="))
                            {
                                strCardInformation = strTrack2Data.Split('=');
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
                                string[] strTrack2Data1  = strCardInformation[1].Split('?');
                                strField35 = strCardInformation[0] + "=" + strTrack2Data1[0].Substring(0, 7);
                            }
                            

                            if(strAgentID.Length == 5)
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "503";
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
                                field24 = "503";
                                strAgentID = "A" + strAgentID;
                            }

                            strAmount = strReceivedData[6];
                           
                            amount = Convert.ToDouble(strAmount);
                           
                            if( amount < 0 )
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Amount must be greater than Zero #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return;
                            }
                            strAccountNumber2 = strReceivedData[7];
                            // strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" & strCardNumber & "'")

                            strPinClear = strReceivedData[5].Replace("Ù", "");
                            strPinClear = strPinClear.Substring(0, 4);

                            if (pinmessagesent == false)
                            {
                                EconnectTMSservice.ClsMain.messagesfrompos.Add(intid, IncomingMessage + "|" + MessageGUID + "|" + strDeviceid);
                                strVerifyPin = main.PIN_Verify("400000", strPinClear, strCardNumber, strExpiryDate, strDeviceid, strField35, intid.PadLeft(12, '0'));

                                opps.spInsertPOSTransaction(intid, strCardNumber, "400000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", "POS FUNDS TRANSFER TO " + strAccountNumber2, "", "", strAgentID, strAccountNumber, strAccountNumber2, "");
                                

                                return;
                            }
                            else
                            {
                                //pin
                                strField37 = field37;
                                strVerifyPin = "00";
                                //for now let the account number from econeect until the switch is able to respond to us
                                //uncomment the 2n line below
                               // strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" + strCardNumber + "'");
                                strAccountNumber = opps.GETACOUNTFROMECONNECT(strCardNumber, strField37);
                                strNarration = "POS FUNDS TRANSFER TO " + strAccountNumber2;
                            }
                            break;
                    }

                ///send to econnect
                    Guid myguid = new Guid(MessageGUID);

                    opps.spInsertPOSTransaction(intid, strCardNumber, "400000", strAmount, "", "", "", field24, "", "", "", strDeviceid, "", "", "", "", strNarration, "", "", strAgentID, strAccountNumber, strAccountNumber2, "");
                    string strRequest  = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "400000", strAmount, field24, strDeviceid, "", "", "", strAgentID, strAccountNumber, strAccountNumber2, "POS", strAgentID, ref myguid,  strNarration ,studentnumber,studentname);
                    main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "Fundstransfer", "Fundstransfer");
            }
        }
    }
}
