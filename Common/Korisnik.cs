using System;

namespace Common
{
    [Serializable]
    public class Korisnik
    {
        public int Id { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Lozinka { get; set; }
        public decimal StanjeNaRacunu { get; set; }

        public Korisnik()
        {
        }

        public Korisnik(int id, string ime, string prezime, string lozinka, decimal pocetnoStanje)
        {
            Id = id;
            Ime = ime;
            Prezime = prezime;
            Lozinka = lozinka;
            StanjeNaRacunu = pocetnoStanje;
        }

        public override string ToString()
        {
            return $"[{Id}] {Ime} {Prezime} - Stanje: {StanjeNaRacunu:N2} RSD";
        }
    }
}
