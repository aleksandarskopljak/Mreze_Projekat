using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Klase
{
    public enum TipTransakcije
    {
        Uplata,
        Isplata,
        Transfer
    }
    public class Transakcija
    {
        public string IdTransakcije { get; set; }
        public TipTransakcije Tip {  get; set; }
        public double Iznos {  get; set; }
        public DateTime Datum { get; set; }

        public Transakcija(string idTransakcije, TipTransakcije tip, double iznos, DateTime datum)
        {
            IdTransakcije = idTransakcije;
            Tip = tip;
            Iznos = iznos;
            Datum = datum;
        }

        public override string ToString()
        {
            return $"Transakcija ID: {IdTransakcije} | Tip: {Tip} | Iznos: {Iznos} | Datum: {Datum}";
        }

    }
}
