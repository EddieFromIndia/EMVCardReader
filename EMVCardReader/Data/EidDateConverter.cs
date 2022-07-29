using System;

namespace EMVCardReader.Data
{
    public static class EidDateConverter
    {
        public static DateTime? GetDateTime(string eidDateString)
        {
            string[] splitDate = eidDateString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (splitDate.Length == 3 && int.TryParse(splitDate[0], out int day) && int.TryParse(splitDate[2], out int year))
            {
                int month = 0;
                switch (splitDate[1].ToUpper())
                {
                    case "JAN":
                        month = 1;
                        break;
                    case "FEV":
                    case "FEB":
                        month = 2;
                        break;
                    case "MARS":
                    case "MAAR":
                    case "MÄR":
                        month = 3;
                        break;
                    case "AVR":
                    case "APR":
                        month = 4;
                        break;
                    case "MAI":
                    case "MEI":
                        month = 5;
                        break;
                    case "JUIN":
                    case "JUN":
                        month = 6;
                        break;
                    case "JUIL":
                    case "JUL":
                        month = 7;
                        break;
                    case "AOUT":
                    case "AUG":
                        month = 8;
                        break;
                    case "SEPT":
                    case "SEP":
                        month = 9;
                        break;
                    case "OCT":
                    case "OKT":
                        month = 10;
                        break;
                    case "NOV":
                        month = 11;
                        break;
                    case "DEC":
                    case "DEZ":
                        month = 12;
                        break;
                    default:
                        break;
                }

                if (month > 0)
                {
                    return new DateTime(year, month, day);
                }
            }
            return null;
        }
    }
}
