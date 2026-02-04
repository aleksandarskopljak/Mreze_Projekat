using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
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
        public TipTransakcije Tip { get; set; }
        public double Iznos { get; set; }
        public DateTime Datum { get; set; }
    }
}
