using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Klase
{
    public class Korisnik
    {
        public String Id { get; set; }
        public String Ime { get; set; }
        public String Prezime { get; set; }
        public double StanjeNaRacunu { get; set; }

        public Korisnik(string id, string ime, string prezime, double stanjeNaRacunu)
        {
            Id = id;
            Ime = ime;
            Prezime = prezime;
            StanjeNaRacunu = 0;
        }

        public override string ToString()
        {
            return $"{Ime} {Prezime} (ID: {Id}) - Stanje: {StanjeNaRacunu}";
        }


    }
}
