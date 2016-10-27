using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconnectTMSservice
{
    class ClsChangePassword
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
                    strAgentCode  = strReceivedData[4].Replace("Ù", "");
                   // if (String.IsNullOrEmpty(strAgentCode))
                    //    strAgentCode = strReceivedData[6].Replace("Ù", "");

                    strAgentPassword = strReceivedData[5].Replace("Ù", "");
                    strAgentPassword = opps.fn_RemoveNon_Numeric(strAgentPassword);
                    Boolean Success  = false;
                    string strnewpassword = strReceivedData[7];
                    string strconfirmpass = strReceivedData[8];

                //check if all parameters ahve been passed

                    if (String.IsNullOrEmpty(strAgentCode) || String.IsNullOrEmpty(strAgentPassword) || String.IsNullOrEmpty(strnewpassword) || String.IsNullOrEmpty(strconfirmpass))
                {
                    strResponse = opps.strResponseHeader(strReceivedData[2]);
                    strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += "   Request for Password Change   " + "#";
                    strResponse += "--------------------------------" + "#";
                    strResponse += " `  Some paramenters missing    " + "#";
                    strResponse += "    Kindly Try Again " + "#";
                    strResponse += opps.strResponseFooter();
                    main.SendPOSResponse(strResponse, MessageGUID);
                    return;
                }

                    strCardNumber = "0000000000000000";
                    strDeviceid = strReceivedData[2];
                   //// strDeviceid = strDeviceid.Substring(0, 15);
                    strExpiryDate = "0000";
                    strAgentID = strReceivedData[4].Trim();
                    strAgentID = opps.fn_RemoveNon_Numeric(strAgentID);
                    if(strAgentID.Length == 5 )
                    {
                        strAgentID = "T" + strAgentID;
                    }
                    else
                    {
                        strAgentID = "A" + strAgentID;
                    }

                    
                    opps.spInsertPOSTransaction(intid, "0000000000000000", "999999", "0", "", intid, "", "", "", "", strField39, strReceivedData[2], "", "", "", "", "CHANGE Password", "", strAgentID, "", strAgentID, "", "");

                    string strInMsg  = "update tbincomingPosTransactions set field_39='00',field_48='Successfull' where field_0='" + intid.PadLeft(12, '0') + "'";
                    Clogic.RunNonQuery(strInMsg);
                  
                    if(strnewpassword != strconfirmpass)
                    {
                        strResponse = opps.strResponseHeader(strReceivedData[2]);
                        strResponse += "Auth ID:      " + intid.PadLeft(12, '0') + "#";
                        strResponse += "--------------------------------" + "#";
                        strResponse += "   Request for Password Change    " + "#";
                        strResponse += "--------------------------------" + "#";
                        strResponse += " `  Password mismatch " + "#";
                        strResponse += "    Kindly Try Again " + "#";
                        strResponse += opps.strResponseFooter();
                        main.SendPOSResponse(strResponse, MessageGUID);
                        return;
                    }
                    else
                    {
                        if(strAgentID.StartsWith("A") )
                        {
                            Success = opps.fn_Updateagentpassword(strAgentID, strnewpassword);
                        }
                        else if(strAgentID.StartsWith("T") )
                        {
                            Success = opps.fn_UpdateTellerPassword(strAgentID, strnewpassword);
                        }

                        if( Success == true)
                        {
                            //'send a postive response
                            strResponse = opps.strResponseHeader(strReceivedData[2]);
                            strResponse += "Auth ID:        " + intid.PadLeft(12, '0') + "#";
                            strResponse += "--------------------------------" + "#";
                            strResponse += "   Request for Password Change    " + "#";
                            strResponse += "--------------------------------" + "#";
                            strResponse += " `  Password change " + strAgentID + "#";
                            strResponse += "    Successful " + "#";
                            strResponse += opps.strResponseFooter();
                    }
            }
                    


                   

                    main.SendPOSResponse(strResponse, MessageGUID);
            }
            catch (Exception ex)
            {
                EconnectTMSservice.ClsMain.LogErrorMessage_Ver1(ex, "changepassword", "changepasswords");
            }
        }
    }
}
