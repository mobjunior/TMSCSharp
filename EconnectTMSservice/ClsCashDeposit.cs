using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsCashDeposit
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
            string strField60 = "";
            string studentnumber = "";
            string studentname ="";
            string studentNarration = "";

            ClsEbankingconnections Clogic = new ClsEbankingconnections();

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
                        strAgentID = strReceivedData[5].Replace("Ù", "");
                        //remove those funny characters from the pos memory thy can be a mess make a transaction fail
                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                        strDeviceid = strReceivedData[2].Replace("Ù", "");
                       //// strDeviceid = strDeviceid.Substring(0, 15);
                        strAccountNumber = strReceivedData[3].Replace("Ù", ""); //fn_getTellerAccountNumber(strAgentID)
                        if (strAgentID.Length == 5)
                        {
                            strAgentID = "T" + strAgentID;
                            field24 = "523";
                            strAmount = strReceivedData[4].Replace("Ù", "");
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
                            field24 = "509";
                            //check if its Agent to Agent and use different code
                            if (strAccountNumber.Substring(0,4) == "4006")
                                field24 = "543";

                            strAmount = strReceivedData[4].Replace("Ù", "");
                            //let verify that an agent does not deposit to his onw float account
                            string stragentfloatacc = opps.fn_getAgentAccountNumber(strAgentID);

                            if (stragentfloatacc == strAccountNumber)
                            {
                                //Transaction not allowed
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "         Cash Deposit           " + "#";
                                strResponse += "--------------------------------" + "#";
                                strResponse += "   Transaction Not Allowed      #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return;
                            }


                        }

                        amount = Convert.ToDouble(strAmount);

                        if (amount < 0)
                        {
                            strResponse = opps.strResponseHeader(strDeviceid);
                            strResponse += "--------------------------------" + "#";
                            strResponse += "Amount must be greater than Zero #";
                            strResponse += opps.strResponseFooter();
                            main.SendPOSResponse(strResponse, MessageGUID);
                            return;
                        }
                        strVerifyPin = "00";
                        strNarration = "POS CASH DEPOSIT-" + strAccountNumber;
                        break;
                    case "SCHOOLFEES":
                        //sProcessingCode, strAgencyCashManagement, SerialNo, strAmountEntered, strAgentCode,AssignAccountNumber,strStudentNumber);
                        //210000#SCHOOLFEES#0000011300031089#2000#1001#5001115133301##

                        strCardNumber = "0000000000000000";
                        strExpiryDate = "0000";
                        strAgentID = strReceivedData[4].Replace("Ù", "");
                        //remove those funny characters from the pos memory thy can be a mess make a transaction fail
                        strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                        strDeviceid = strReceivedData[2].Replace("Ù", "");
                       //// strDeviceid = strDeviceid.Substring(0, 15);
                        strAccountNumber = strReceivedData[5].Replace("Ù", ""); //fn_getTellerAccountNumber(strAgentID)
                        studentnumber = strReceivedData[6].Replace("Ù", "");
                        //studentname = strReceivedData[7].Replace("Ù", "");
                        studentNarration = strReceivedData[7].Replace("Ù", "");
                        strAmount = strReceivedData[3].Replace("Ù", "");
                        //verify the shool account number
                        string schoolacc = opps.GetShooldetails(strAccountNumber);
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
                        //studentname = opps.Verifystudentdetails(schoolacc, studentnumber);
                        //if (String.IsNullOrEmpty(studentname))
                        //{
                        //    strResponse = opps.strResponseHeader(strDeviceid);
                        //    strResponse += "--------------------------------" + "#";
                        //    strResponse += "student number does not exists #";
                        //    strResponse += opps.strResponseFooter();
                        //    main.SendPOSResponse(strResponse, MessageGUID);
                        //    return;
                        //}

                        if (strAgentID.Length == 5)
                        {
                            strAgentID = "T" + strAgentID;
                            field24 = "534";
                           
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
                            field24 = "533";
                            //strAmount = strReceivedData[5].Replace("Ù", "");
                        }

                        amount = Convert.ToDouble(strAmount);

                        if (amount < 0)
                        {
                            strResponse = opps.strResponseHeader(strDeviceid);
                            strResponse += "--------------------------------" + "#";
                            strResponse += "Amount must be greater than Zero #";
                            strResponse += opps.strResponseFooter();
                            main.SendPOSResponse(strResponse, MessageGUID);
                            return;
                        }
                        strVerifyPin = "00";
                        // strNarration = "POS SCHOOL FEE PAYMENT-" + strAccountNumber + " " + studentname + " " + studentnumber; 
                        strNarration = "POS SCHOOL FEE PAYMENT-" + strAccountNumber + " " + studentnumber+" "+ studentNarration ;
                          
                        break;
                    case "CASHDEPOSIT": // E.g IZone Teller Picks Cash from Izone Cashier.. i.e Cashier deposits money with banks teller at IZONE
                            strCardNumber = "0000000000000000";
                            strExpiryDate = "0000";

                            strAgentCode = strReceivedData[3].Replace("Ù", "");
                            strAgentCode = opps.fn_RemoveNon_Numeric(strAgentCode);
                            strAgentPassword  = strReceivedData[4].Replace("Ù", "");

                            strAgentID = strReceivedData[6].Replace("Ù", "");
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                            strDeviceid = strReceivedData[2].Replace("Ù", "");
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strAccountNumber = opps.fn_getTellerAccountNumber(strAgentID.Trim());

                            if(strAgentID.Length == 5)
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "502";
                                strAmount = strReceivedData[5].Replace("Ù", "");
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
                                strAmount = strReceivedData[5].Replace("Ù", "");
                            }

                           
                            amount = Convert.ToDouble(strAmount);
                            //if amount 
                            if(amount < 0 )
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Amount must be greater than Zero #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return;
                            }
                            strVerifyPin = "00";
                            //strNarration = "POS CASH DEPOSIT-" & strAccountNumber
                            //julius changed 08022013
                            strNarration = "POS CASH COLLECTION-" + strAccountNumber;

                           break;
                    case "CASHPICKUP": // When Bank comes to Collect the Money at IZONE PREMISE
                            strCardNumber = "0000000000000000";
                            strExpiryDate = "0000";

                             strAgentCode  = strReceivedData[3].Replace("Ù", "");
                            strAgentCode = opps.fn_RemoveNon_Numeric(strAgentCode);
                             strAgentPassword  = strReceivedData[4].Replace("Ù", "");
                            strAgentPassword = opps.fn_RemoveNon_Numeric(strAgentPassword);

                           
                            strAgentID = strReceivedData[6].Replace("Ù", "");
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                            strDeviceid = strReceivedData[2].Replace("Ù", "");
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strAccountNumber = opps.fn_getTellerTransferAccount(strAgentID.Trim());
                            if(strAgentID.Length == 5)
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "505";
                                strAmount = strReceivedData[5].Replace("Ù", "");
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
                                strAmount = strReceivedData[5].Replace("Ù", "");
                            }
                           
                            amount = Convert.ToDouble(strAmount);
                            
                            if(amount < 0 )
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Amount must be greater than Zero #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                                return ;
                            }

                            strVerifyPin = "00";
                            strNarration = "POS CASH PICKUP-" + strAccountNumber;
                        break;
                    case "KITSPAYMENTS": // When Accepting KITS money for sFSA's/Agents/Supervisors. These guys will receive kits when opening their accounts and need to be paid...
                            /// This will be at the banks tellrs menu....
                            strCardNumber = "0000000000000000";
                            strExpiryDate = "0000";

                            strAgentID = strReceivedData[4].Replace("Ù", "");
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);

                            strDeviceid = strReceivedData[2].Replace("Ù", "");
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strAccountNumber = strReceivedData[3].Replace("Ù", "");
                            if(strAgentID.Length == 5)
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "524";
                                strAmount = opps.fn_get_kits_price();
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
                                strAmount = opps.fn_get_kits_price();
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
                            strNarration = "POS KIT FEES COLLECTION-" + strAccountNumber;
                        break;
                    case "LOANREPAYMENTS":
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
                            else
                            {
                                field24 = "508";
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
                           // strField60 = "LOANREPAYMENT";

                        break;
                    case "CASHACCEPT":// ' This is at the IZONE Premise when they need to receive money from the branch to facilitate their transactions...
                            // Teller Implant at IZONE will accept that they have received money from the bank

                            strCardNumber = "0000000000000000";
                            strExpiryDate = "0000";

                             strAgentCode  = strReceivedData[3].Replace("Ù", "");
                            strAgentCode = opps.fn_RemoveNon_Numeric(strAgentCode);
                            strAgentPassword  = strReceivedData[4].Replace("Ù", "");

                            strAgentID = strReceivedData[6].Replace("Ù", "");
                            strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                            strDeviceid = strReceivedData[2].Replace("Ù", "");
                           //// strDeviceid = strDeviceid.Substring(0, 15);
                            strAccountNumber = opps.fn_getTellerTransferAccount(strAgentID.Trim());
                            if(strAgentID.Length == 5)
                            {
                                strAgentID = "T" + strAgentID;
                                field24 = "523" ; //Cash Acceptance from the Branch for the Banks Teller at IZONE
                                strAmount = strReceivedData[5].Replace("Ù", "");
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
                                field24 = "509";
                                strAgentID = "A" + strAgentID;
                                strAmount = strReceivedData[5].Replace("Ù", "");
                            }
                            
                            amount = Convert.ToDouble(strAmount);
                            
                            if(amount < 0)
                            {
                                strResponse = opps.strResponseHeader(strDeviceid);
                                strResponse += "--------------------------------" + "#";
                                strResponse += "Amount must be greater than Zero #";
                                strResponse += opps.strResponseFooter();
                                main.SendPOSResponse(strResponse, MessageGUID);
                             return;
                            }
                            strVerifyPin = "00";
                            strNarration = "POS ACCEPT CASH FROM BRANCH-" + strAccountNumber;
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
                            strVerifyPin = main.PIN_Verify("210000", strPinClear, strCardNumber, strExpiryDate, strDeviceid, strField35, intid.PadLeft(12, '0'));
                           
                            opps.spInsertPOSTransaction(intid, strCardNumber, "210000", strAmount, "", "", "", field24, "", "", "", strReceivedData[2], "", "", "", "", strNarration, "", "", strAgentID, "", strAccountNumber, "");
                            return;
                        }
                        else
                        {
                            //pin done and successful
                                                     
                            string strField37 = field37;
                            strVerifyPin = "00";
                            //for now let the account number from econeect until the switch is able to respond to us
                            //uncomment the 2n line below
                            //strAccountNumber = Clogic.RunStringReturnStringValue("select AccountNo from tbAccountSequence where CardNo='" + strCardNumber + "'");
                            strAccountNumber = opps.GETACOUNTFROMECONNECT(strCardNumber, strField37);
                            strNarration = "POS CASH DEPOSIT-" + strAccountNumber;   
                            strField37 = strField37.PadLeft(12, '0');
                            Guid myguid = new Guid(MessageGUID);
                            string strRequest = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "210000", strAmount, field24, strDeviceid, "", "", "", strAgentID, "", strAccountNumber, "POS", strAgentID,ref myguid, strNarration);
                            main.SendToEconnect(strRequest, intid, MessageGUID, strDeviceid, strCardNumber);
                            return;
                            //wait for reply from econnect on socket arrival for processed transaction
                        }

                        break;
                }//end of switch

                opps.spInsertPOSTransaction(intid, strCardNumber, "210000", strAmount, "", "", "", field24, "", "", "", strReceivedData[2], "", "", "", "", strNarration, "", "", strAgentID, "", strAccountNumber, "");
                Guid myguid1 = new Guid(MessageGUID);
                string strRequesttoEconect = opps.GenerateXMLtoeConnect(intid, "0200", strCardNumber, "210000", strAmount, field24, strDeviceid, "", "", "", strAgentID, "", strAccountNumber, "POS", strAgentID, ref myguid1, strNarration,studentnumber,studentNarration);
                main.SendToEconnect(strRequesttoEconect, intid, MessageGUID, strDeviceid, strCardNumber);
                            
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "Cashdeposit", "cashdeposit");
            }
        }
    }
}
