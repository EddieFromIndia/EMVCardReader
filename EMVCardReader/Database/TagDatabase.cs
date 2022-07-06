﻿using System;
using System.Collections.Generic;

namespace EMVCardReader
{
    public static class TagDatabase
    {
        public static Dictionary<string, List<string>> Tags = new Dictionary<string, List<string>>()
        {                               // Name, P/C, Display Format (String/Byte Array)
            { "42", new List<string>() { "Issuer Identification Number (IIN)", "Primitive", "ByteArray" } },
            { "4F", new List<string>() { "Application File Identifier (AID)", "Primitive", "ByteArray" } },
            { "50", new List<string>() { "Application Label", "Primitive", "String" } },
            { "57", new List<string>() { "Track 2 Equivalent Data", "Primitive", "String" } },
            { "5A", new List<string>() { "Application Primary Account Number (PAN)", "Primitive", "String" } },
            { "5D", new List<string>() { "Directory Definition File (DDF) Name", "Primitive", "String" } },
            { "5F20", new List<string>() { "Cardholder Name", "Primitive", "String" } },
            { "5F24", new List<string>() { "Application Expiration Date", "Primitive", "String" } },
            { "5F25", new List<string>() { "Application Effective Date", "Primitive", "String" } },
            { "5F28", new List<string>() { "Issuer Country Code", "Primitive", "String" } },
            { "5F2A", new List<string>() { "Transaction Currency Code", "Primitive", "String" } }, // Also called Transaction Country Code
            { "5F2D", new List<string>() { "Language Preference", "Primitive", "String" } },
            { "5F30", new List<string>() { "Service Code", "Primitive", "String" } },
            { "5F34", new List<string>() { "Application Primary Account Number (PAN) Sequence Number", "Primitive", "String" } },
            { "5F36", new List<string>() { "Transaction Currency Exponent", "Primitive", "String" } },
            { "5F50", new List<string>() { "Issuer URL", "Primitive", "String" } },
            { "5F53", new List<string>() { "International Bank Account Number (IBAN)", "Primitive", "String" } },
            { "5F54", new List<string>() { "Bank Identifier Code (BIC)", "Primitive", "String" } },
            { "5F55", new List<string>() { "Issuer Country Code (alpha2 format)", "Primitive", "String" } },
            { "5F56", new List<string>() { "Issuer Country Code (alpha3 format)", "Primitive", "String" } },
            { "5F57", new List<string>() { "Account Type", "Primitive", "String" } },
            { "61", new List<string>() { "Application Template", "Constructed", "ByteArray" } },
            { "6F", new List<string>() { "File Control Information (FCI) Template", "Constructed", "ByteArray" } },
            { "70", new List<string>() { "Application Data File (ADF)", "Constructed", "ByteArray" } },
            { "71", new List<string>() { "Issuer Script Template 1", "Constructed", "ByteArray" } },
            { "72", new List<string>() { "Issuer Script Template 2", "Constructed", "ByteArray" } },
            { "73", new List<string>() { "Directory Discretionary Template", "Constructed", "ByteArray" } },
            { "77", new List<string>() { "Response Message Template Format 2", "Constructed", "ByteArray" } },
            { "80", new List<string>() { "Response Message Template Format 1", "Constructed", "ByteArray" } },
            { "81", new List<string>() { "Amount Authorised (Binary)", "Primitive", "String" } },
            { "82", new List<string>() { "Application Interchange Profile (AIP)", "Primitive", "ByteArray" } },
            { "83", new List<string>() { "Command Template", "Primitive", "ByteArray" } },
            { "84", new List<string>() { "Dedicated File (DF) Name", "Primitive", "ByteArray" } },
            { "86", new List<string>() { "Issuer Script Command", "Primitive", "String" } },
            { "87", new List<string>() { "Application Priority Indicator", "Primitive", "ByteArray" } },
            { "88", new List<string>() { "Short File Identifier (SFI)", "Primitive", "ByteArray" } },
            { "89", new List<string>() { "Authorisation Code", "Primitive", "ByteArray" } },
            { "8A", new List<string>() { "Authorisation Response Code", "Primitive", "ByteArray" } },
            { "8C", new List<string>() { "Card Risk Management Data Object List 1 (CDOL1)", "Primitive", "ByteArray" } },
            { "8D", new List<string>() { "Card Risk Management Data Object List 1 (CDOL2)", "Primitive", "ByteArray" } },
            { "8E", new List<string>() { "Cardholder Verification Method (CVM) List", "Primitive", "ByteArray" } },
            { "8F", new List<string>() { "Certification Authority Public Key Index", "Primitive", "ByteArray" } },
            { "90", new List<string>() { "Issuer Public Key Certificate", "Primitive", "ByteArray" } },
            { "91", new List<string>() { "Issuer Authentication Data", "Primitive", "ByteArray" } },
            { "92", new List<string>() { "Issuer Public Key Remainder", "Primitive", "ByteArray" } },
            { "93", new List<string>() { "Signed Static Application Data", "Primitive", "ByteArray" } },
            { "94", new List<string>() { "Application File Locator (AFL)", "Constructed", "ByteArray" } },
            { "95", new List<string>() { "Terminal Verification Results", "Primitive", "ByteArray" } },
            { "97", new List<string>() { "Transaction Certificate Data Object List (TDOL)", "Primitive", "ByteArray" } },
            { "98", new List<string>() { "Transaction Certificate (TC) Hash Value", "Primitive", "ByteArray" } },
            { "99", new List<string>() { "Transaction Personal Identification Number (PIN) Data", "ByteArray" } },
            { "9A", new List<string>() { "Transaction Date", "Primitive", "String" } },
            { "9B", new List<string>() { "Transaction Status Information", "Primitive", "String" } },
            { "9C", new List<string>() { "Transaction Type", "Primitive", "String" } },
            { "9D", new List<string>() { "Directory Definition File (DDF) Name", "Primitive", "String" } },
            { "9F01", new List<string>() { "Acquirer Identifier", "Primitive", "String" } },
            { "9F02", new List<string>() { "Amount, Authorised (Numeric)", "Primitive", "String" } },
            { "9F03", new List<string>() { "Amount, Other (Numeric)", "Primitive", "String" } },
            { "9F04", new List<string>() { "Amount, Other (Binary)", "Primitive", "String" } },
            { "9F05", new List<string>() { "Application Discretionary Data", "Primitive", "ByteArray" } },
            { "9F06", new List<string>() { "Application Identifier (AID)", "Primitive", "String" } },
            { "9F07", new List<string>() { "Application Usage Control", "Primitive", "String" } },
            { "9F08", new List<string>() { "Application Version Number", "Primitive", "String" } },
            { "9F09", new List<string>() { "Application Version Number", "Primitive", "String" } },
            { "9F0B", new List<string>() { "Cardholder Name Extended", "Primitive", "String" } },
            { "9F0D", new List<string>() { "Issuer Action Code - Default", "Primitive", "String" } },
            { "9F0E", new List<string>() { "Issuer Action Code - Denial", "Primitive", "String" } },
            { "9F0F", new List<string>() { "Issuer Action Code - Online", "Primitive", "String" } },
            { "9F10", new List<string>() { "Issuer Application Data", "Primitive", "ByteArray" } },
            { "9F11", new List<string>() { "Issuer Code Table Index - Default", "Primitive", "ByteArray" } },
            { "9F12", new List<string>() { "Application Preferred Name", "Primitive", "String" } },
            { "9F13", new List<string>() { "Last Online ATC Register", "Primitive", "String" } },
            { "9F14", new List<string>() { "Lower Consecutive Offline Limit (LCOL)", "Primitive", "String" } },
            { "9F15", new List<string>() { "Merchant Category Code", "Primitive", "String" } },
            { "9F16", new List<string>() { "Merchant Identifier", "Primitive", "String" } },
            { "9F17", new List<string>() { "Personal Identification Number (PIN) Try Counter", "Primitive", "String" } },
            { "9F18", new List<string>() { "Issuer Script Identifier", "Primitive", "String" } },
            { "9F19", new List<string>() { "Dynamic Data Authentication Data Object List (DDOL)", "Primitive", "String" } },
            { "9F1A", new List<string>() { "Terminal Country Code", "Primitive", "String" } },
            { "9F1B", new List<string>() { "Terminal Floor Limit", "Primitive", "String" } },
            { "9F1C", new List<string>() { "Terminal Identification", "Primitive", "String" } },
            { "9F1D", new List<string>() { "Terminal Risk Management Data", "Primitive", "ByteArray" } },
            { "9F1E", new List<string>() { "Interface Device (IFD) Serial Number", "Primitive", "String" } },
            { "9F1F", new List<string>() { "Track 1 Discretionary Data", "Primitive", "ByteArray" } },
            { "9F20", new List<string>() { "Track 2 Discretionary Data", "Primitive", "ByteArray" } },
            { "9F21", new List<string>() { "Transaction Time", "Primitive", "String" } },
            { "9F22", new List<string>() { "Certification Authority Public Key Index", "Primitive", "String" } },
            { "9F23", new List<string>() { "Upper Consecutive Offline Limit (UCOL)", "Primitive", "String" } },
            { "9F26", new List<string>() { "Application Cryptogram", "Primitive", "ByteArray" } },
            { "9F27", new List<string>() { "Cryptogram Information Data", "Primitive", "ByteArray" } },
            { "9F2D", new List<string>() { "ICC PIN Encipherment Public Key Certificate", "Primitive", "ByteArray" } },
            { "9F2E", new List<string>() { "ICC PIN Encipherment Public Key Exponent", "Primitive", "String" } },
            { "9F2F", new List<string>() { "ICC PIN Encipherment Public Key Remainder", "Primitive", "String" } },
            { "9F32", new List<string>() { "Issuer Public Key Exponent", "Primitive", "String" } },
            { "9F33", new List<string>() { "Terminal Capabilities", "Primitive", "String" } },
            { "9F34", new List<string>() { "Cardholder Verification Method (CVM) Results", "Primitive", "String" } },
            { "9F35", new List<string>() { "Terminal Type", "Primitive", "String" } },
            { "9F36", new List<string>() { "Application Transaction Counter (ATC)", "Primitive", "String" } },
            { "9F37", new List<string>() { "Unpredictable Number", "Primitive", "String" } },
            { "9F38", new List<string>() { "Processing Options Data Object List (PDOL)", "Constructed", "ByteArray" } },
            { "9F39", new List<string>() { "Point-of-Service (POS) Entry Mode", "Primitive", "String" } },
            { "9F3A", new List<string>() { "Amount, Reference Currency", "Primitive", "String" } },
            { "9F3B", new List<string>() { "Application Reference Currency", "Primitive", "String" } },
            { "9F3C", new List<string>() { "Transaction Reference Currency Code", "Primitive", "String" } },
            { "9F3D", new List<string>() { "Transaction Reference Currency Exponent", "Primitive", "String" } },
            { "9F40", new List<string>() { "Additional Terminal Capabilities", "Primitive", "ByteArray" } },
            { "9F41", new List<string>() { "Transaction Sequence Counter", "Primitive", "String" } },
            { "9F42", new List<string>() { "Application Currency Code", "Primitive", "String" } },
            { "9F43", new List<string>() { "Application Reference Currency Exponent", "Primitive", "String" } },
            { "9F44", new List<string>() { "Application Currency Exponent", "Primitive", "String" } },
            { "9F45", new List<string>() { "Data Authentication Code", "Primitive", "ByteArray" } },
            { "9F46", new List<string>() { "ICC Public Key Certificate", "Primitive", "ByteArray" } },
            { "9F47", new List<string>() { "ICC Public Key Exponent", "Primitive", "String" } },
            { "9F48", new List<string>() { "ICC Public Key Remainder", "Primitive", "String" } },
            { "9F49", new List<string>() { "Dynamic Data Authentication Data Object List (DDOL)", "Constructed", "ByteArray" } },
            { "9F4A", new List<string>() { "Static Data Authentication Tag List", "Constructed", "ByteArray" } },
            { "9F4B", new List<string>() { "Signed Dynamic Application Data", "Primitive", "ByteArray" } },
            { "9F4C", new List<string>() { "ICC Dynamic Number", "Primitive", "String" } },
            { "9F4D", new List<string>() { "Log Entry", "Primitive", "String" } },
            { "9F4E", new List<string>() { "Merchant Name and Location", "Primitive", "String" } },
            { "9F4F", new List<string>() { "Log Format", "Primitive", "String" } },
            { "9F51", new List<string>() { "Application Currency Code", "Primitive", "String" } },
            { "9F52", new List<string>() { "Card Verification Results (CVR)", "Primitive", "String" } },
            { "9F53", new List<string>() { "Consecutive Transaction Limit (International)", "Primitive", "String" } },
            { "9F54", new List<string>() { "Cumulative Total Transaction Amount Limit", "Primitive", "String" } },
            { "9F55", new List<string>() { "Geographic Indicator", "Primitive", "String" } },
            { "9F56", new List<string>() { "Issuer Authentication Indicator", "Primitive", "String" } },
            { "9F57", new List<string>() { "Issuer Country Code", "Primitive", "String" } },
            { "9F58", new List<string>() { "Lower Consecutive Offline Limit (Card Check)", "Primitive", "String" } },
            { "9F59", new List<string>() { "Upper Consecutive Offline Limit (Card Check)", "Primitive", "String" } },
            { "9F5A", new List<string>() { "Issuer URL2", "Primitive", "String" } },
            { "9F5C", new List<string>() { "Cumulative Total Transaction Amount Upper Limit", "Primitive", "String" } },
            { "9F72", new List<string>() { "Consecutive Transaction Limit (International - Country)", "Primitive", "String" } },
            { "9F73", new List<string>() { "Currency Conversion Factor", "Primitive", "String" } },
            { "9F74", new List<string>() { "VLP Issuer Authorization Code", "Primitive", "String" } },
            { "9F75", new List<string>() { "Cumulative Total Transaction Amount Limit - Dual Currency", "Primitive", "String" } },
            { "9F76", new List<string>() { "Secondary Application Currency Code", "Primitive", "String" } },
            { "9F77", new List<string>() { "VLP Funds Limit", "Primitive", "String" } },
            { "9F78", new List<string>() { "VLP Single Transaction Limit", "Primitive", "String" } },
            { "9F79", new List<string>() { "VLP Available Funds", "Primitive", "String" } },
            { "9F7F", new List<string>() { "Card Production Life Cycle (CPLC) History File Identifiers", "Primitive", "String" } },
            { "A5", new List<string>() { "FCI Proprietary Template", "Constructed", "ByteArray" } },
            { "BF0C", new List<string>() { "File Control Information (FCI) Issuer Discretionary Data", "Constructed", "ByteArray" } }
        };
    }
}