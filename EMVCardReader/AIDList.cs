using System.Collections.Generic;

namespace EMVCardReader
{
    public static class AIDList
    {
        public static List<string> List = new List<string>(){
          "A0000000031010",        // Visa credit or debit
          "A000000003101001",      // VISA Credit
          "A000000003101002",      // VISA Debit
          "A0000000032010",        // Visa electron
          "A0000000032020",        // V pay
          "A0000000033010",        // VISA Interlink
          "A0000000034010",        // VISA Specific
          "A0000000035010",        // VISA Specific
          "A0000000036010",        // Domestic Visa Cash Stored Value
          "A0000000036020",        // International Visa Cash Stored Value
          "A0000000038002",        // Barclays/HBOS
          "A0000000038010",        // Visa Plus
          "A0000000039010",        // VISA Loyalty
          "A000000003999910",      // VISA ATM
          "A000000004",            // US Debit (MC)
          "A0000000041010",        // Mastercard credit or debit
          "A00000000410101213",    // MasterCard
          "A00000000410101215",    // MasterCard
          "A000000004110101213",   // Mastercard Credit
          "A0000000042010",        // MasterCard Specific
          "A0000000043010",        // MasterCard Specific
          "A0000000043060",        // Mastercard Maestro
          "A0000000044010",        // MasterCard Specific
          "A0000000045010",        // MasterCard Specific
          "A0000000046000",        // Mastercard Cirrus
          "A0000000048002",        // NatWest or SecureCode Auth
          "A0000000049999",        // Mastercard
          "A0000000050001",        // UK Domestic Maestro - Switch (debit card) Maestro
          "A0000000050002",        // UK Domestic Maestro - Switch (debit card) Solo
          "A0000000250000",        // American Express (Credit/Debit)
          "A00000002501",          // American Express
          "A000000025010402",      // American Express
          "A000000025010701",      // American Express ExpressPay
          "A000000025010801",      // American Express
          "A0000000291010",        // ATM card LINK (UK) ATM network
          "A0000000421010",        // French CB
          "A0000000422010",        // French CB
          "A00000006510",          // JCB
          "A0000000651010",        // JCB
          "A00000006900",          // FR Moneo
          "A000000098",            // US Debit (Visa)
          "A0000000980848",        // Schwab Bank Debit Card
          "A0000001211010",        // Dankort Danish domestic debit card
          "A0000001410001",        // Pagobancomat Italian domestic debit card
          "A000000152",            // US Debit (Discover)
          "A0000001523010",        // Diners club/Discover
          "A0000001544442",        // Banricompras Debito (Brazil)
          "A0000001850002",        // UK Post Office Card Account card
          "A0000002281010",        // SAMA Saudi Arabia domestic credit/debit card
          "A0000002282010",        // SAMA Saudi Arabia domestic credit/debit card
          "A0000002771010",        // Interac
          "A0000003156020",        // Chipknip
          "A0000003241010",        // Discover
          "A0000003591010028001",  // ZKA Girocard (Germany)
          "A0000003710001",        // InterSwitch Verve Card (Nigeria)
          "A0000004540010",        // Etranzact Genesis Card (Nigeria)
          "A0000004540011",        // Etranzact Genesis Card 2 (Nigeria)
          "A0000004766C",          // Google
          "A0000005241010",        // RuPay (India)
          "A000000620",            // US Debit (DNA)
          "D27600002545500100",    // ZKA Girocard (Germany)
          "D5780000021010",        // BankAxept Norwegian domestic debit card
        };
    }
}
