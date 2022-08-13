using System;

 namespace  Cobra_III_Rfm69_Test01
{
    class X_Stellig
    {
        public static string Zahl(string pZahlString, int pStellen)
        {
            string ZahlString = pZahlString;
            while (ZahlString.Length < pStellen)
            {
                ZahlString = "0" + ZahlString;
            }
            return ZahlString;
        }
    }
}
